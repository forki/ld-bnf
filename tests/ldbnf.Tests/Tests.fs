module ldbnf.Tests
open NUnit.Framework
open FSharp.RDF
open Assertion
open rdf
open Bnf.DrugRdf
open resource
open Bnf.Order

[<Test>]
let ``Ensure that addOrder adds the order property to a single resource`` () =
  let po =
   (P !!"base:pUri", O(Node.Uri !!"base:oUri", lazy[resource !!"base:rUri"
     [ dataProperty !!"base:someDataProperty" ("dataValue"^^xsd.string) ]
     ]))

  let count = ref 0
  let newPo = addOrder po count 

  match newPo with
  | (P p, O(Node.Uri u, resources)) ->

    let resource = 
      resources.Force() 
      |> List.head
      |> getResource 

    let property =
      resource.Statements
      |> List.head
      |> getDataProperty 

    Assert.AreEqual(!!"nicebnf:hasOrder", property.Uri)
    Assert.AreEqual("1", property.Value)

[<Test>]
let ``Ensure that addOrder adds order to multiple resources and increments order`` () =
  let po =
   (P !!"base:pUri", O(Node.Uri !!"base:oUri", lazy[
         resource !!"base:rUri1" [ dataProperty !!"base:someDataProperty" ("dataValue"^^xsd.string) ]
         resource !!"base:rUri2" [ dataProperty !!"base:someDataProperty" ("dataValue"^^xsd.string) ]]))

  let count = ref 0
  let newPo = addOrder po count

  match newPo with
  | (P p, O(Node.Uri u, resources)) ->

    let resources = 
      resources.Force() 
      |> List.map getResource

    let statements =
      resources
      |> List.map (fun s-> s.Statements)

    let resourceOneProperty = 
      List.nth statements 0
      |> List.head
      |> getDataProperty

    let resourceTwoProperty = 
      List.nth statements 1
      |> List.head
      |> getDataProperty

    Assert.AreEqual(!!"nicebnf:hasOrder", resourceOneProperty.Uri)
    Assert.AreEqual("1", resourceOneProperty.Value)

    Assert.AreEqual(!!"nicebnf:hasOrder", resourceTwoProperty.Uri)
    Assert.AreEqual("2", resourceTwoProperty.Value)

[<Test>]
let ``Ensure that addOrder adds order to a nested resource and increments order`` () =
  let po =
   (P !!"base:pUri", O(Node.Uri !!"base:oUri", lazy[
         resource !!"base:rUri" 
            [ one !!"base:nUri" !!"base:nested" [ dataProperty !!"base:someDataProperty" ("dataValue"^^xsd.string)]]
         ]))

  let count = ref 0
  let newPo = addOrder po count

  match newPo with
  | (P p, O(Node.Uri u, resources)) ->

    let resources = 
      resources.Force() 
      |> List.map getResource

    let statements =
      resources
      |> List.map (fun s-> s.Statements)

    let nestedResources = 
      List.nth statements 0
      |> List.tail 
      |> List.head

    let nestedResource = 
      match nestedResources with
        | (P p, O(Node.Uri u, resources)) -> resources.Force() |> List.map getResource

    let statements =
      nestedResource
      |> List.map (fun s-> s.Statements)

    let property =
      List.nth statements 0
      |> List.head
      |> getDataProperty

    Assert.AreEqual(!!"nicebnf:hasOrder", property.Uri)
    Assert.AreEqual("2", property.Value)

[<Test>]
let ``Ensure that addOrder adds order to a blank node``() = 
  let po =   
   (P !!"base:pUri", O(Node.Uri !!"base:oUri", lazy[resource !!"base:rUri"
     [ blank !!"base:someBlankProperty"
         [ dataProperty !!"base:someBlankDataProperty" ("blankValue"^^xsd.string)]]]))
  
  let count = ref 0
  let newPo = addOrder po count

  match newPo with
  | (P p, O(Node.Uri u, resources)) ->
    Assert.AreEqual(!!"base:pUri", p)
    Assert.AreEqual(!!"base:oUri", u)

    let resource =
      resources.Force()
      |> List.head
      |> getResource

    let (bUri, bStatements) = 
      List.nth resource.Statements 1
      |> getBlankNodeFrom

    let property =
      bStatements
      |> List.head
      |> getDataProperty

    Assert.AreEqual(!!"nicebnf:hasOrder", property.Uri)
    Assert.AreEqual("2", property.Value)

[<Test>]
let ``Ensure that addOrder adds order to multipe blank nodes and increments order``() = 
  let po =   
   (P !!"base:pUri", O(Node.Uri !!"base:oUri", lazy[resource !!"base:rUri"
     [ blank !!"base:someBlankProperty1" [ dataProperty !!"base:someBlankDataProperty" ("blankValue"^^xsd.string) ]
       blank !!"base:someBlankProperty2" [ dataProperty !!"base:someBlankDataProperty" ("blankValue"^^xsd.string) ]]]))
  
  let count = ref 0
  let newPo = addOrder po count

  match newPo with
  | (P p, O(Node.Uri u, resources)) ->
    Assert.AreEqual(!!"base:pUri", p)
    Assert.AreEqual(!!"base:oUri", u)

    let resource =
      resources.Force()
      |> List.head
      |> getResource

    let (bnAUri, bnAStatements) = 
      List.nth resource.Statements 1
      |> getBlankNodeFrom
   
    let bnAProperty =
      List.nth bnAStatements 0
      |> getDataProperty 

    Assert.AreEqual(!!"nicebnf:hasOrder", bnAProperty.Uri)
    Assert.AreEqual("2", bnAProperty.Value)

    let (bnBUri, bnBStatements) = 
      List.nth resource.Statements 2
      |> getBlankNodeFrom
   
    let bnBProperty =
      List.nth bnBStatements 0
      |> getDataProperty 

    Assert.AreEqual(!!"nicebnf:hasOrder", bnBProperty.Uri)
    Assert.AreEqual("3", bnBProperty.Value)

[<Test>]
let ``Ensure that addOrder maintains the existing structure`` () =

  let po =
   (P !!"base:pUri", O(Node.Uri !!"base:oUri", lazy[resource !!"base:rUri"
     [ blank !!"base:someBlankProperty"
         [ dataProperty !!"base:someBlankDataProperty" ("blankValue"^^xsd.string)]
       dataProperty !!"base:someDataProperty" ("dataValue"^^xsd.string) ]
     ]))

  let count = ref 0
  let newPo = addOrder po count

  match newPo with
  | (P p, O(Node.Uri u, resources)) ->
    Assert.AreEqual(!!"base:pUri", p)
    Assert.AreEqual(!!"base:oUri", u)

    let resource = 
      resources.Force() 
      |> List.head
      |> getResource 

    Assert.AreEqual(!!"base:rUri", resource.Uri)

    let property =
      List.nth resource.Statements 2
      |> getDataProperty 

    Assert.AreEqual(!!"base:someDataProperty", property.Uri)
    Assert.AreEqual("dataValue", property.Value)

    let (bUri, bStatements) = 
      List.nth resource.Statements 1
      |> getBlankNodeFrom

    Assert.AreEqual(!!"base:someBlankProperty", bUri)

    let property =
      List.nth bStatements 1
      |> getDataProperty 

    Assert.AreEqual(!!"base:someBlankDataProperty", property.Uri)
    Assert.AreEqual("blankValue", property.Value)