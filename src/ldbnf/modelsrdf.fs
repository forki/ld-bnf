namespace Bnf
open FSharp.RDF
open FSharp.Data.Runtime
open prelude
open resource
open Assertion
open Rdf
open Shared
open rdf
open RdfUris


module MedicalDeviceRdf =
  open MedicalDevice

  type Graph with
    static member frommedicaldevice(MedicalDevice(id,t,pdi,ids)) =
      let s = optionlist {
        yield a Uri.MedicalDeviceEntity
        yield t |> (string >> label)
        yield pdi >>= (ditastr >> Some)
        yield! ids |> List.map (Uri.frommdt >> (objectProperty !!"nicebnf:hasMedicalDeviceType"))
        }
      let dr = resource (Uri.frommd id)
      [dr s] |> Assert.graph (empty())

module BorderlineSubstanceTaxonomyRdf =
  open BorderlineSubstanceTaxonomy

  type Graph with
    static member from(x:BorderlineSubstanceTaxonomy) =
      let l t = match t with | Title t -> t.XElement.Value.ToString()

      let s = optionlist {
        yield a Uri.BorderlineSubstanceTaxonomyEntity
        yield x.title |> (l >> label)
        yield x.general >>= (ditastr >> Some)
        yield! x.substances |> List.map (Uri.frombsc >> (objectProperty !!"nicebnf:hasBorderlineSubstance"))
        yield! x.categories |> List.map (Uri.frombst >> (objectProperty !!"nicebnf:hasBorderlineSubstanceTaxonomy"))
        }

      let dr = resource (Uri.frombst x.id)
      [dr s]
      |> Assert.graph (empty())

module PublicationRdf = 
  open Bnf.Publication

  type Graph with
    static member fromPublication (Publication(d,wmid)) =
      let dto = (System.DateTimeOffset d)^^xsd.datetime

      let wm wmid = one !!"nicebnf:hasWoundManagement" (Uri.fromwm wmid)
                      [a !!(Uri.nicebnfClass + "WoundManagementRoot")]

      let s = [a !!(Uri.nicebnf + "Publication")
               dto |> (dataProperty !!"nicebnf:hasPublicationDate")
               wmid |> wm]

      let dr = resource !!(Uri.bnfsite + "publication")
      [dr s]
      |> Assert.graph (empty())

module GenericRdf = 
  open Bnf.Generic

  type Graph with
    static member fromti (Title s) = s |> label

    static member fromta (TargetAudience s) =
      dataProperty !!"nicebnf:hasTargetAudience" (s^^xsd.string)

    static member fromcontent (Content(s,ta)) =
      blank !!"nicebnf:hasContent"
        (optionlist {
          yield ta >>= (Graph.fromta >> Some)
          yield s |> dita})

    static member from (x:ContentLink) =
      objectProperty !!"nicebnf:hasLink" (Uri.totopic (x.rel,x.id))

    static member fromGeneric n (x:Generic) =
      let uri n id = !!(sprintf "%s%s/%s" Uri.bnfsite n (string id))

      let s = optionlist {
        yield a !!(Uri.nicebnf + n)
        yield Graph.fromti x.title
        yield! x.links |> Seq.map Graph.from |> Seq.toList
        yield! x.content |> List.map Graph.fromcontent}

      let dr r = resource (uri n x.id) r

      [dr s]
      |> Assert.graph (empty())

module IndexRdf = 
  open Bnf.Index

  type Graph with
    static member fromindex n (Index(id,ids)) =

      let uri n id = !!(sprintf "%s%s/%s" Uri.bnfsite n (string id))

      let s = optionlist {
        yield a !!(sprintf "%s%ss" Uri.nicebnf n)
        yield! ids |> List.map ((uri n) >> objectProperty !!("nicebnf:has" + n))
        }

      let dr r = resource (uri (sprintf "%ss" n) id) r

      [dr s]
      |> Assert.graph (empty())


module InteractionRdf =
  open Bnf.Interaction

  type Graph with
    static member from (InteractionList(id,t,il,ids,n)) =
      let note (Note(p,t)) = blank !!"nicebnf:hasNote"
                              [t |> (toString >> xsd.string >> dataProperty !!"nicebnf:hasNoteType")
                               p |> dita]
      let s = optionlist{
                yield a Uri.InteractionListEntity
                yield t.XElement.Value |> label
                yield t.XElement |> (string >> title)
                yield n >>= (note >> Some)}

      let iwuri = Uri.fromiw id

      let importance i =
        match i.importance with
          | High -> dataProperty !!"nicebnf:hasImportance" ("High"^^xsd.string)
          | NotSet -> dataProperty !!"nicebnf:hasImportance" ("NotSet"^^xsd.string)

      let interactionDetail i = one !!"nicebnf:hasInteraction" (iwuri i)
                                 [a Uri.InteractionEntity
                                  objectProperty !!"nicebnf:interactsWith" (Uri.fromiwl i)
                                  importance i
                                  i.message |> dita
                                  i.message.XElement.Value |> (string >> label)
                                  dataProperty !!"nicebnf:hasImportance" ((string i.importance)^^xsd.string)]

      let link = Uri.fromil >> objectProperty !!"nicebnf:hasInteractionList"

      let dr r = resource (Uri.fromil id) r
      [dr s
       dr (il |> List.map interactionDetail)
       dr (ids |> List.map link)]
       |> Assert.graph (empty())

module BorderlineSubstanceRdf =
  open Bnf.BorderlineSubstance

  let inline dpo n x = x >>= (string >> xsd.string >> (dataProperty !!("nicebnf:has" + n)) >> Some)

  type Graph with
    static member from (x:BorderlineSubstance) =
      let l t = match t with | Title t -> t.XElement.Value.ToString()

      let s =  optionlist {
                yield a Uri.BorderlineSubstanceEntity
                yield x.title |> (l >> label)
                yield x.title |> (string >> title)
                yield x.category |> (string >> Uri.frombst >> (objectProperty !!"nicebnf:hasCategory"))
                yield x.intro >>= (string >> xsd.string >> (dataProperty !!"nicebnf:hasIntroductoryNote") >> Some)}

      let ds = x.details |> List.map Graph.fromdetails

      let dr r = resource (Uri.from x) r
      [dr s
       dr ds]
       |> Assert.graph (empty())


    static member frompackinfo (PackInfo(ps,uom,acbs)) =
      optionlist {
        yield ps |> dpo "PackSize"
        yield uom |> dpo "UnitOfMeasure"
        yield acbs |> dpo "Acbs"}

    static member fromnhsindicativeinfo (NhsIndicativeInfo(nhsi,pt,nhsip)) =
      optionlist {
        yield nhsi |> dpo "NhsIndicative"
        yield pt |> dpo "PriceText"
        yield nhsip |> dpo "NhsIndicativePrice"}

    static member frompricetarrif (PackSizePriceTariff(pi,nhs)) =
      let s = [pi >>= (Graph.frompackinfo >> Some)
               nhs >>= (Graph.fromnhsindicativeinfo >> Some)] |> List.choose id
      blank !!"nicebnf:hasPack" (s |> List.collect id)

    static member fromprep (BorderlineSubstancePrep(t,pts)) =
      let s = match t with
              | Some (PreparationTitle(p,m)) ->
                optionlist {
                  yield p |> (string >> title)
                  yield p.XElement.Value |> label
                  yield m |> dpo "Manufacturer"}
              | None -> []
      let ts = pts |> List.map Graph.frompricetarrif
      blank !!"nicebnf:hasBorderlineSubstancePrep" (s @ ts)

    static member fromdetail (x:Detail) =
      let inline dp n s = dataProperty !!("nicebnf:has" + n) ((string s)^^xsd.string)
      let inline dpx n s = dataProperty !!("nicebnf:has" + n) ((string s)^^xsd.xmlliteral)
      match x with
        | Formulation s -> s |> dp "Formulation"
        | EnergyKj e -> e |> dp "EnergyKj"
        | EnergyKcal e -> e |> dp "EnergyKcal"
        | ProteinGrams p -> p |> dp "ProteinGrams"
        | ProteinConstituents p -> p |> dp "ProteinConstituents"
        | CarbohydrateGrams c -> c |> dp "CarbohydrateGrams"
        | CarbohydrateConstituents c -> c |> dp "CarbohydrateConstituents"
        | FatGrams f -> f |> dp "FatGrams"
        | FatConstituents f -> f |> dp "FatConstituents"
        | FibreGrams f -> f |> dp "FibreGrams"
        | SpecialCharacteristics s -> s |> dp "SpecialCharacteristics"
        | Acbs a -> a |> dpx "Acbs"
        | Presentation p -> p |> dp "Presentation"

    static member fromdetails (Details(ds,bsps)) =
      let dps = ds |> List.map Graph.fromdetail
      let preps = bsps |> List.map Graph.fromprep
      blank !!"nicebnf:hasDetails" (dps @ preps)


module DrugClassificationRdf =
  open DrugClassification

  type Graph with
    static member from (DrugClassifications cs) =
      cs |> List.map Graph.from |> Assert.graph (empty())

    static member from (x:Classification) =
      resource !!(Uri.nicebnfClass + "Classification#" + x.key) [x.value |> label]

module TreatmentSummaryRdf =
  open Bnf.TreatmentSummary

  type Graph with
    static member from (x:TreatmentSummary) =
      let isa  (TreatmentSummary (_,x)) =
        match x with
          | Generic _ ->  a Uri.TreatmentSummaryEntity
          | _ -> a !!(Uri.nicebnf + (toString x))

      let s = optionlist {
               yield isa x}

      let p = Graph.fromts x
      let dr r = resource (Uri.from x) r
      [dr s
       dr p] |> Assert.graph (empty())

    static member fromti (Title s) = s |> label
    static member fromdoi (Shared.Doi s) =
      dataProperty !!"nicebnf:hasDoi" (s^^xsd.string)
    static member frombs (BodySystem x) =
        one !!"nicebnf:hasBodySystem" (Uri.frombs x) (optionlist {
          yield a Uri.BodySystemEntity
          yield dataProperty !!"rdfs:label" (x^^xsd.string)
          })

    static member fromta (TargetAudience s) =
      dataProperty !!"nicebnf:hasTargetAudience" (s^^xsd.string)
    static member fromcontent (Content(s,ta)) =
      blank !!"nicebnf:hasContent"
       (optionlist {
         yield ta >>= (Graph.fromta >> Some)
         yield s |> dita})

    static member from (x:ContentLink) =
      objectProperty !!"nicebnf:hasLink" (Uri.totopic (x.rel,x.id))

    static member from (x:Summary) =
      optionlist {
        yield Graph.fromti x.title
        yield x.doi >>= (Graph.fromdoi >> Some)
        yield x.bodySystem >>= (Graph.frombs >> Some)
        yield! x.links |> Seq.map Graph.from |> Seq.toList
        yield! x.content |> List.map Graph.fromcontent}

    static member fromts (TreatmentSummary (_,x)) =
      match x with
        | ComparativeInformation s -> Graph.from s
        | ManagementOfConditions s -> Graph.from s
        | MedicalEmergenciesBodySystems s -> Graph.from s
        | TreatmentOfBodySystems s -> Graph.from s
        | About s -> Graph.from s
        | Guidance s -> Graph.from s
        | Generic s -> Graph.from s

module MedicinalFormRdf =
  open Bnf.Drug
  open Bnf.MedicinalForm

  type Graph with
    static member from (x:MedicinalForm) =
      let s = optionlist{
                yield a Uri.MedicinalFormEntity
                yield x.title >>= (string >> label >> Some)
                yield x.excipients >>= Graph.fromexc
                yield x.electrolytes >>= Graph.fromele}

      let cals = match x.cautionaryAdvisoryLabels with
                 | Some x -> Graph.fromcals x
                 | None -> []

      let cmpis = x.cmpis |> List.map Graph.fromclinicalmpi

      let mps = x.medicinalProducts |> List.map Graph.from
      let dr r = resource (Uri.from x) r
      [dr s
       dr mps
       dr cals
       dr cmpis]
       |> Assert.graph (empty())

    static member fromclinicalmpi x = objectProperty !!"nicebnf:hasClinicalMedicinalProductInformation" (Uri.fromcmpi x)

    static member fromcal (CautionaryAdvisoryLabel(ln,p)) =
      blank !!"nicebnf:hasCautionaryAdvisoryLabel"
               (optionlist {
                 yield p |> dita
                 yield ln >>= (string >> xsd.string >> (dataProperty !!"nicebnf:hasLabelNumber") >> Some)})

    static member fromcals (CautionaryAdvisoryLabels(_,cals)) =
      cals |> Array.map Graph.fromcal |> Array.toList

    static member dp n = xsd.string >> (dataProperty !!("nicebnf:has" + n))

    static member fromman (Manufacturer x) = Graph.dp "Manufacturer" x |> Some
    static member frombt (BlackTriangle x) = Graph.dp "BlackTriangle" x |> Some
    static member frommpt (MedicinalProductTitle(m,bt,t)) =
      let s = optionlist {
               yield m >>= Graph.fromman
               yield bt >>= Graph.frombt
               yield t |> dita}
      blank !!"nicebnf:hasMedicinalProductTitle" s |> Some

    static member fromexc (Excipients e) =
      e |> (string >> xsd.xmlliteral >> (dataProperty !!"nicebnf:hasExcipients") >> Some)

    static member fromele (Electrolytes e) =
      e |> (string >> xsd.xmlliteral >> (dataProperty !!"nicebnf:hasElectrolytes") >> Some)

    static member fromsai(StrengthOfActiveIngredient p) = Graph.dp "StrengthOfActiveIngredient" (string p) |> Some
    static member fromcd(ControlledDrug p) = Graph.dp "ControlledDrug" (string p) |> Some

    static member fromnhsi (NhsIndicative x) = Graph.dp "NhsIndicative" x |> Some
    static member frompt (PriceText x) = Graph.dp "PriceText" x |> Some
    static member fromnhsip (NhsIndicativePrice x) = Graph.dp "NhsIndicativePrice" (string x) |> Some
    static member fromhos (Hospital x) = Graph.dp "Hospital" x |> Some
    static member fromnhsii (NhsIndicativeInfo(nhsi,pt,nhsip,hos)) =
      optionlist {
        yield nhsi >>= Graph.fromnhsi
        yield pt >>= Graph.frompt
        yield nhsip >>= Graph.fromnhsip
        yield hos >>= Graph.fromhos}

    static member fromps (PackSize d) = Graph.dp "PackSize" (string d) |> Some
    static member fromuom u = Graph.dp "UnitOfMeasure" (string u) |> Some
    static member fromlc lc = Graph.dp "LegalCategory" (string lc) |> Some
    static member fromacbs (Acbs x) = Graph.dp "Acbs" x |> Some
    static member frompackinfo (PackInfo(ps,uom,lc,acbs)) =
      optionlist {
       yield ps >>= Graph.fromps
       yield uom >>= Graph.fromuom
       yield lc >>= Graph.fromlc
       yield acbs >>= Graph.fromacbs}

    static member fromdt (DrugTarrif s) = Graph.dp "DrugTarrif" s |> Some
    static member fromdtp (DrugTariffPrice dtp) = Graph.dp "DrugTariffPrice" (string dtp) |> Some
    static member fromdti (DrugTariffInfo(dt,pt,dtp)) =
      optionlist {
       yield dt >>= Graph.fromdt
       yield pt >>= Graph.frompt
       yield dtp >>= Graph.fromdtp}

    static member frompack(Pack(pi,nii,dti)) =
      blank !!"nicebnf:hasPack"
        (optionlist {
          return! pi >>= (Graph.frompackinfo >> Some)
          return! nii >>= (Graph.fromnhsii >> Some)
          return! dti >>= (Graph.fromdti >> Some)
          yield a !!"nicebnf:Pack"})

    static member from (x:MedicinalProduct) =
      one !!"nicebnf:hasMedicinalProduct" (Uri.from x)
        (optionlist {
          yield a Uri.MedicinalProductEntity
          yield x.ampid |> string |> Graph.dp "Ampid"
          yield x.title |> Graph.frommpt
          yield! x.strengthOfActiveIngredient |> List.choose Graph.fromsai
          yield! x.controlledDrugs |> List.choose Graph.fromcd
          yield! x.packs |> List.map Graph.frompack
          })


module WoundManagementRdf =
  open Bnf.WoundManagement

  let dp n = xsd.string >> dataProperty !!("nicebnf:has" + n)

  type Graph with
    static member setupGraph = Graph.ReallyEmpty ["nicebnf",!!Uri.nicebnf
                                                  "rdfs",!!"http://www.w3.org/2000/01/rdf-schema#"
                                                  "bnfsite",!!Uri.bnfsite]
    static member fromTitle (Title t) = t |> label

    static member from (x:WoundManagement) =
      let s = optionlist {
               yield a Uri.WoundManagementEntity
               yield x.general >>= (string >> (dp "General") >> Some)
               yield x.title |> Graph.fromTitle
               yield! x.dressingChoices |> List.map Graph.fromWoundType
               yield! x.productGroups |> List.map Graph.fromProductGroup
               yield! x.links |> List.map Graph.fromwml
              }

      let dr r = resource (Uri.from x) r

      [dr s]
      |> Assert.graph Graph.setupGraph

    static member fromDescription (Description sd) = sd |> dita

    static member fromwml (x:WoundManagementLink) =
      objectProperty !!"nicebnf:hasWoundManagement" (Uri.from x)

    static member fromExudate (WoundExudate(s,wmls)) =
      blank !!"nicebnf:WoundExudate"
        (dataProperty !!"nicebnf:hasRate" (s^^xsd.string) :: (wmls |> List.map Graph.fromwml))

    static member fromWoundType (WoundType(TypeOfWound(t),d,wes)) =
      blank !!"nicebnf:hasWoundType"
        (optionlist {
            yield t |> dp "TypeOfWound"
            yield d >>= (Graph.fromDescription >> Some)
            yield! wes |> List.map Graph.fromExudate
          })

    static member fromProduct (x:Product) =
      one !!"nicebnf:hasProduct" (Uri.from x)
       [x.manufacturer |> dp "Manufacturer"
        x.name |> dp "Name"
        x.price |> (string >> (dp "Price"))]

    static member fromProductGroup (ProductGroup(t,d,pl)) =
      blank !!"nicebnf:hasProductGroup"
        (optionlist {
            yield t |>  Graph.fromTitle
            yield d >>= (Graph.fromDescription >> Some)
            yield! pl |> List.map Graph.fromProduct
            })



module MedicalDeviceTypeRdf =
  open Bnf.Drug
  open Bnf.MedicinalForm
  open Bnf.MedicalDeviceType
  open DrugRdf
  open MedicinalFormRdf

  type Graph with
    static member from (x:MedicalDeviceType) =
      let s = [a Uri.MedicalDeviceTypeEntity
               x.title |> (string >> label)]

      let uri = Uri.fromcmdig x

      let gs = x.groups |> List.map (Graph.fromcmdig uri)

      let dr r = resource (Uri.from x) r
      [dr s
       dr gs]
       |> Assert.graph Graph.setupGraph

    static member fromcmdig uri (x:ClinicalMedicalDeviceInformationGroup) =
      let s =  optionlist {
                yield a Uri.ClinicalMedicalDeviceInformationGroupEntity
                yield x.title |> (string >> label)
                yield x.description >>= (Graph.fromdd uri >> Some)
                yield x.complicance >>= (Graph.fromcs uri >> Some)}

      let sec = Graph.fromsec uri

      let ss = x.sections |> List.collect sec
      let mps = x.products |> List.map Graph.from

      one !!"bnfsite:hasClinicalMedicalDeviceInformationGroup" (uri x.id) (s @ ss @ mps)

    static member fromdd uri (DeviceDescription(id,sd)) =
      one !!"bnfsite:hasDeviceDescription" (uri id) [sd |> dita]

    static member fromcs uri (ComplicanceStandards(id,sd)) =
      one !!"bnfsite:hasComplicanceStandards" (uri id) [sd |> dita]
