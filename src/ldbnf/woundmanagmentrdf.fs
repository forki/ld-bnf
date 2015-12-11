namespace Bnf
open FSharp.RDF
open FSharp.Data.Runtime

module WoundManagementRdf =
  open prelude
  open resource
  open Bnf.WoundManagement
  open Assertion
  open rdf
  open Shared
  open Rdf
  open RdfUris

  type Graph with
    static member setupGraph = Graph.ReallyEmpty ["nicebnf",!!Uri.nicebnf
                                                  "rdfs",!!"http://www.w3.org/2000/01/rdf-schema#"
                                                  "bnfsite",!!Uri.bnfsite]
    static member fromTitle (Title t) = dataProperty !!"nicebnf:hasTitle" (t^^xsd.string)

    static member from (x:WoundManagement) =
      let s = optionlist {
               yield a Uri.WoundManagementEntity
               yield x.general >>= (string >> xsd.string >> (dataProperty !!"bnfsite:hasGeneral") >> Some)
               yield x.title |> Graph.fromTitle
               yield! x.dressingChoices |> List.map Graph.fromWoundType
               yield! x.productGroups |> List.map Graph.fromProductGroup
              }

      let dr r = resource (Uri.from x) r

      [dr s]
      |> Assert.graph Graph.setupGraph

    static member fromDescription (Description sd) =
      dataProperty !!"bnfsite:hasDitaContent" (sd |> (string >> xsd.string))

    static member fromwml (x:WoundManagementLink) =
      objectProperty !!"nicebnf:hasWoundManagment" (Uri.totopic(x.rel,x.id))

    static member fromExudate (WoundExudate(s,wmls)) =
      blank !!"nicebnf:WoundExudate"
        (dataProperty !!"nicebnf:Rate" (s^^xsd.string) :: (wmls |> List.map Graph.fromwml))

    static member fromWoundType (WoundType(TypeOfWound(t),d,wes)) =
      blank !!"nicebnf:hasWoundType"
        (optionlist {
            yield t |> (xsd.string >> dataProperty !!"nicebnf:hasTypeOfWound")
            yield d >>= (Graph.fromDescription >> Some)
            yield! wes |> List.map Graph.fromExudate
          })

    //some of this might be pointless when the apmid points to a product?
    static member fromProduct (x:Product) =
      blank !!"nicebnf:hasProduct"
       [x.manufacturer |> (xsd.string >> dataProperty !!"nicebnf:hasManufacturer")
        x.name |> (xsd.string >> dataProperty !!"nicebnf:hasName")
        x.price |> (xsd.string >> dataProperty !!"nicebnf:hasPrice")
        x.ampid |> (Uri.fromampid >> objectProperty !!"nicebnf:hasAmpid")]

    static member fromProductGroup (ProductGroup(t,d,pl)) =
      blank !!"nicebnf:hasProductGroup"
        (optionlist {
            yield t |>  Graph.fromTitle
            yield d >>= (Graph.fromDescription >> Some)
            yield! pl |> List.map Graph.fromProduct
          })
