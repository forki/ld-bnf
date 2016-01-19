namespace Bnf

module prelude = 
    let (|?) = defaultArg

    let (>=>) a b x =
      match (a x) with
        | Some x -> b x
        | None -> None

    let (>>|) a b x =
      match (a x) with
        | Some x -> Some(b x)
        | None -> None

    let (>>=) a b = Option.bind b a

    let (<!>) a b = Option.map b a

    //lift2 : ('a -> 'b -> 'c) -> 'a option -> 'b option -> 'c option
    type Option<'a> with
      static member lift2 f x y =
        match x with 
        | None -> None
        | Some x' -> 
            match y with
            | None -> None
            | Some y' -> Some <| f x' y'


    //Generic list that allows optional values to collapse out
    type OptionlistBuilder<'a> () =
        member this.Bind(m, f) = m |> List.collect f
        member this.Zero() = []
        member this.Yield(x:'a option) = match x with | Some x -> [x] | None -> [] //collapse None into an empty list
        member this.Yield(x:'a) = [x]
        member this.YieldFrom(x:'a list) = x
        member this.ReturnFrom(x:'a list option) = match x with | Some x -> x | None -> []  //naughty but convenient
        member this.Combine (a,b) = List.concat [a;b]
        member this.Delay(f) = f()

    open Microsoft.FSharp.Reflection

    //get a string reprasentaiton of a DU
    let toString (x:'a) = 
      match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let fromString<'a> (s:string) =
      match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
        |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
        |_ -> None

    //deal with names and output class properties on type providers
    let inline name arg =
        ( ^a : (member Name : string) arg)

    let inline hasName s x =
        if (name x) = s then Some(x)
        else None

    let inline (|HasName|_|) n x = hasName n x

    let inline outputclasso  arg =
        ( ^a : (member Outputclass : Option<string>) arg)

    let inline outputclass arg =
        ( ^a : (member Outputclass : string) arg) 

    let overlap s (o:string) x = 
      if (o.Split [|' '|] |> Array.exists (fun  c -> c = s)) then Some(x)
      else None

    let inline hasOutputclass (s:string) x = overlap s (outputclass x) x

    let inline hasOutputclasso (s:string) x =
        match outputclasso x with
          | Some o -> overlap s o x
          | None -> None

    let inline (|HasOutputClass|_|) (n:string) x = hasOutputclass n x

    let inline (|HasOutputClasso|_|) (n:string) x = hasOutputclasso n x




