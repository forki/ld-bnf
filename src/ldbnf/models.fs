namespace Bnf
open FSharp.Data
open prelude
open Shared

module MedicinalForm =

  type LabelNumber =
    | LabelNumber of decimal
    override __.ToString() = match __ with | LabelNumber x -> string x

  type CautionaryAdvisoryLabel = | CautionaryAdvisoryLabel of Option<LabelNumber> * drugProvider.P

  type CautionaryAdvisoryLabels = | CautionaryAdvisoryLabels of drugProvider.Title * CautionaryAdvisoryLabel []

  type Excipients = | Excipients of drugProvider.Section

  type Electrolytes = | Electrolytes of drugProvider.Section

  type Manufacturer = | Manufacturer of string

  type BlackTriangle = | BlackTriangle of string

  type MedicinalProductTitle = | MedicinalProductTitle of Manufacturer option * BlackTriangle option * drugProvider.Title

  type Ampid =
    | Ampid of int64
    override __.ToString() = match __ with | Ampid x -> string x

  type StrengthOfActiveIngredient = | StrengthOfActiveIngredient of drugProvider.P

  type UnitOfMeasure =
    | UnitOfMeasure of string
    override __.ToString() = match __ with | UnitOfMeasure x -> x

  type PackSize = | PackSize of decimal

  type LegalCategory =
    | POM
    | P
    | GSL
    override __.ToString() = toString __

  type Acbs = | Acbs of string

  type PackInfo = | PackInfo of PackSize option * UnitOfMeasure option * LegalCategory option * Acbs option

  type PriceText = | PriceText of string

  type NhsIndicative = | NhsIndicative of string

  type NhsIndicativePrice = | NhsIndicativePrice of decimal

  type Hospital = | Hospital of string

  type NhsIndicativeInfo = | NhsIndicativeInfo of NhsIndicative option * PriceText option * NhsIndicativePrice option * Hospital option

  type DrugTarrif = | DrugTarrif of string

  type DrugTariffPrice = | DrugTariffPrice of decimal

  type DrugTariffInfo = | DrugTariffInfo of DrugTarrif option * PriceText option * DrugTariffPrice option

  type ControlledDrug = | ControlledDrug of drugProvider.P

  type Pack = | Pack of PackInfo option * NhsIndicativeInfo option * DrugTariffInfo option * drugProvider.Li

  type ClinicalMedicinalProductInformation = | ClinicalMedicinalProductInformation of Id

  type MedicinalProduct = {
    title:MedicinalProductTitle;
    ampid:Ampid;
    strengthOfActiveIngredient: StrengthOfActiveIngredient list;
    controlledDrugs: ControlledDrug list;
    packs: Pack list}

  type MedicinalForm = {
    id : Id;
    title : drugProvider.Title;
    excipients : Excipients option;
    electrolytes : Electrolytes option;
    cautionaryAdvisoryLabels : CautionaryAdvisoryLabels option;
    medicinalProducts : MedicinalProduct list;
    cmpis : ClinicalMedicinalProductInformation list;
  }

module MedicinalFormParser =
  open MedicinalForm

  let inline withoc n x =
    let oc = (^a : (member Outputclass : string) x)
    if (oc = n) then Some (x)
    else None

  let inline withoco n x =
    let oc = (^a : (member Outputclass : Option<string>) x)
    match oc with
      | Some s when s = n -> Some x
      | _ -> None

  type CautionaryAdvisoryLabel with
    static member from (x:drugProvider.P) =
      let ln = x.Phs.[0].Number <!> LabelNumber
      CautionaryAdvisoryLabel(ln,x)

  type CautionaryAdvisoryLabels with
    static member from (x:drugProvider.Section) =
      let ls = x.Ps |> Array.map CautionaryAdvisoryLabel.from
      match x.Title with
            | Some t -> CautionaryAdvisoryLabels(t,ls)
            | None -> failwith "CautionaryAdvisoryLabels must have a Title"

  let fromphn c (x:drugProvider.Ph) = x.Number <!> c

  let fromphs c (x:drugProvider.Ph) = x.String <!> c

  type UnitOfMeasure with
    static member from (x:drugProvider.Ph) = x.String <!> UnitOfMeasure

  type LegalCategory with
    static member from (x:drugProvider.Ph) =
      let tolc = function
                 | "POM" -> Some POM
                 | "P" -> Some P
                 | "GSL" -> Some GSL
                 | _ -> None
      x.String >>= tolc

  type PackInfo with
    static member from (x:drugProvider.P) =
      let ps = x.Phs |> Array.tryPick (withoc "packSize") >>= (fromphn PackSize)
      let uom = x.Phs |> Array.tryPick (withoc "unitOfMeasure") >>= UnitOfMeasure.from
      let lc = x.Phs |> Array.tryPick (withoc "legalCategory") >>= LegalCategory.from
      let acbs = x.Phs |> Array.tryPick (withoc "acbs") >>= (fromphs Acbs)
      PackInfo(ps,uom,lc,acbs)

  type NhsIndicativeInfo with
    static member from (x:drugProvider.P) =
      let nhsi = x.Phs |> Array.tryPick (withoc "nhsIndicative") >>= (fromphs NhsIndicative)
      let pt = x.Phs |> Array.tryPick (withoc "priceText") >>= (fromphs PriceText)
      let nhsip = x.Phs |> Array.tryPick (withoc "nhsIndicativePrice") >>= (fromphn NhsIndicativePrice)
      let hos = x.Phs |> Array.tryPick (withoc "hospital") >>= (fromphs Hospital)
      NhsIndicativeInfo(nhsi,pt,nhsip,hos)

  type DrugTariffInfo with
    static member from (x:drugProvider.P) =
      let dt = x.Phs |> Array.tryPick (withoc "drugTariff") >>= (fromphs DrugTarrif)
      let pt = x.Phs |> Array.tryPick (withoc "priceText") >>= (fromphs PriceText)
      let dtp = x.Phs |> Array.tryPick (withoc "drugTariffPrice") >>= (fromphn DrugTariffPrice)
      DrugTariffInfo(dt,pt,dtp)

  type MedicinalProductTitle with
    static member from (x:drugProvider.Title) =
      let mph = x.Phs |> Array.tryPick (withoc "manufacturer")
      let btph = x.Phs |> Array.tryPick (withoc "blackTriangle")

      let m =  mph >>= (fromphs Manufacturer)
      let bt = btph >>= (fromphs BlackTriangle)
      if (mph.IsSome) then mph.Value.XElement.Remove()
      if (btph.IsSome) then btph.Value.XElement.Remove()

      MedicinalProductTitle(m,bt,x)

  type Ampid with
    static member from (x:drugProvider.Data) =
      Ampid(x.Number.Value)

  //add a uri
  type Pack with
    static member from (x:drugProvider.Ul) =
      x.Lis |> Array.map Pack.from
    static member from (x:drugProvider.Li) =
      let pi = x.Ps |> Array.tryPick (withoco "packInfo") <!> PackInfo.from
      let nio = x.Ps |> Array.tryPick (withoco "nhsIndicativeInfo") <!> NhsIndicativeInfo.from
      let dti = x.Ps |> Array.tryPick (withoco "drugTariffInfo") <!> DrugTariffInfo.from
      Pack(pi,nio,dti,x)

  type MedicinalProduct with
    static member from (x:drugProvider.Section) =
      let t = match x.Title with
               | Some t -> MedicinalProductTitle.from t
               | None -> failwith "MedicinalProduct must have a Title"
      let str = x.Ps |> Array.choose (withoco "strengthOfActiveIngredient" >> Option.map StrengthOfActiveIngredient)
                     |> Array.toList
      let con = x.Ps |> Array.choose (withoco "controlledDrugs" >> Option.map ControlledDrug)
                     |> Array.toList
      let ps = x.Uls |> Array.collect Pack.from |> Array.toList
      let a = match x.Data with
              | Some d -> Ampid.from d
              | None -> failwith "MedicinalProduct must have an Ampid"
      {title = t; ampid = a; strengthOfActiveIngredient = str; packs = ps; controlledDrugs = con;}

  type ClinicalMedicinalProductInformation with
    static member list (x:drugProvider.Section) =
      x.Xrefs |> Array.map (fun xref -> xref.Href |> Id |> ClinicalMedicinalProductInformation)

  type MedicinalForm with
    static member parse (x:drugProvider.Topic) =
      let sections (x:drugProvider.Topic) =
        match x.Body with
        | Some (b) -> b.Sections
        | _ -> failwith "body is required"

      let t = x.Title

      let cals = x |> sections
                   |> Array.tryPick (withoco "cautionaryAndAdvisoryLabels" >> Option.map CautionaryAdvisoryLabels.from)
      let mps = x |> sections
                  |> Array.choose (withoco "medicinalProduct" >> Option.map MedicinalProduct.from)
                  |> Array.toList
      let ex = x |> sections
                 |> Array.tryPick (withoco "excipients" >> Option.map Excipients)
      let el = x |> sections
                 |> Array.tryPick (withoco "fluidAndElectrolytes" >> Option.map Electrolytes)
      let cmpis = x |> sections
                    |> Array.choose (withoco "clinicalMedicinalProductInformationGroup")
                    |> Array.collect ClinicalMedicinalProductInformation.list
                    |> Array.toList
      {id = Id(x.Id); title = t; excipients=ex; electrolytes=el; cautionaryAdvisoryLabels = cals; medicinalProducts = mps; cmpis = cmpis;}

module ClinicalMedicalDeviceInformationGroup =
  open Drug
  open MedicinalForm

  type DeviceDescription = | DeviceDescription of Id * drugProvider.Sectiondiv

  type ComplicanceStandards = | ComplicanceStandards of Id * drugProvider.Sectiondiv

  type ClinicalMedicalDeviceInformationGroup ={
     id:Id;
     title:drugProvider.Title;
     sections:MonographSection list;
     products:MedicinalProduct list;
     description:DeviceDescription option;
     complicance:ComplicanceStandards option }

module ClinicalMedicalDeviceInformationGroupParser =
  open ClinicalMedicalDeviceInformationGroup
  open Drug
  open DrugParser
  open MedicinalForm
  open MedicinalFormParser

  let sections (x:drugProvider.Topic) =
    match x.Body with
    | Some (b) -> b.Sections
    | _ -> failwith "body is required"

  let firstsd c (x:drugProvider.Topic) =
    let sd = x |> sections |> Array.collect (fun s -> s.Sectiondivs)
    c(Id(x.Id),sd.[0])

  type DeviceDescription with
    static member from  = firstsd DeviceDescription

  type ComplicanceStandards with
    static member from = firstsd ComplicanceStandards

  type ClinicalMedicalDeviceInformationGroup with
    static member parse (x:drugProvider.Topic) =
      let mss = x.Topics |> Array.map MonographSection.section |> Array.choose id |> Array.toList

      let mps = x.Topics
                 |> Array.choose (withoc "clinicalMedicinalProducts")
                 |> Array.collect sections
                 |> Array.choose (withoco "medicinalProduct")
                 |> Array.map MedicinalProduct.from
                 |> Array.toList

      let des = x.Topics |> Array.tryPick (withoc "deviceDescription") <!> DeviceDescription.from
      let com = x.Topics |> Array.tryPick (withoc "complianceStandards") <!> ComplicanceStandards.from

      {id=Id(x.Id); title=x.Title; sections=mss; products=mps; description = des; complicance = com;}

module MedicalDeviceType =
  open Drug
  open MedicinalForm

  type MedicalDeviceType = {
     id:Id
     title:drugProvider.Title
     products:MedicinalProduct list
     groups: Href list
    }

module MedicalDeviceTypeParser =
  open Drug
  open DrugParser
  open MedicinalForm
  open MedicinalFormParser
  open MedicalDeviceType

  type MedicalDeviceType with
    static member parse (x:drugProvider.Topic) =
      let gs = x.Topics
               |> Array.collect (fun t -> t.Xrefs)
               |> Array.map (fun x -> x.Href |> Href)
               |> Array.toList
      let products = match x.Body with
                     | Some b -> b.Sections
                                 |> Array.choose (withoco "medicinalProduct")
                                 |> Array.map MedicinalProduct.from
                                 |> Array.toList
                     | _ -> []
      {id=Id(x.Id); title=x.Title; products=products; groups=gs;}

open System.Xml.Linq
open System.Xml.XPath

module TreatmentSummary =
  type tsProvider = XmlProvider<"../../samples/supertreatmentsummary.xml", Global=true>

  type Label = {
    number:string option
    recommendation:tsProvider.P option
    description:tsProvider.P list
    }

  type TargetAudience = | TargetAudience of string
  type Content =
    | Content of tsProvider.Section * TargetAudience option
    | LabelContent of Label

  type BodySystem = | BodySystem of string

  type Summary = {
    title:tsProvider.Title
    doi:Doi option
    bodySystem:BodySystem option
    content:Content list
    links:ContentLink seq
    sublinks: tsProvider.Xref list
    indexlinks: Id list
  }

  type Index = {
    indexlinks: Id list
    topicType: string option
  }

  type Treatment =
    | ComparativeInformation of Summary
    | ManagementOfConditions of Summary
    | MedicalEmergenciesBodySystems of Summary
    | TreatmentOfBodySystems of Summary
    | About of Summary
    | Guidance of Summary
    | Generic of Summary
    | AboutIndex of Index
    | GuidanceIndex of Index

  type TreatmentSummary = | TreatmentSummary of Id * Treatment

module TreatmentSummaryParser =
  open TreatmentSummary

  let withname = (|HasName|_|)

  type Doi with
    static member from (x:tsProvider.Data) = Doi(x.Value)

  type BodySystem with
    static member from (x:tsProvider.Data) = BodySystem(x.Value)

  type Content with
    static member from (x:tsProvider.Section) =
      let label (s:tsProvider.Section) = {
         number = s.Ps |> Array.tryPick ((hasOutputclasso "number") >> Option.bind (fun p -> p.String))
         recommendation = s.Ps |> Array.tryPick (hasOutputclasso "recommendation")
         description = s.Ps |> Array.choose (hasOutputclasso "labelDescription") |> Array.toList
        }

      match x.Outputclass with
      | Some "label" -> x |> label |> LabelContent
      | Some ta -> Content(x,TargetAudience ta |> Some)
      | None -> Content(x,None)

  type Summary with
    static member from (x:tsProvider.Topic) =
      let ls = x.XElement |> ContentLink.from
      let d = x.Body.Datas |> Array.tryPick (withname "doi" >> Option.map Doi.from)
      let bs = x.Body.Datas |> Array.tryPick (withname "bodySystem" >> Option.map BodySystem.from)
      let c = x.Body.Sections |> Array.map Content.from |> Array.toList

      Id(x.Id),{title = x.Title; doi = d; bodySystem = bs; content = c; links = ls; sublinks = x.Body.Xrefs |> Array.toList; indexlinks = []}

    static member fromIndex (x:tsProvider.Topic) =
      let href (x:XElement) =
        let href = x.Attribute(XName.Get "href").Value
        Id(href) |> Some
      let topicType = x.XElement.XPathSelectElement("//topic").Attribute(XName.Get "bnfid").Value
      Id(x.Id),{indexlinks = x.XElement.XPathSelectElements("//xref") |> Seq.choose href |> Seq.toList; topicType = Some(topicType)}
  
  type TreatmentSummary with
    static member from c (i,s) = TreatmentSummary(i, c s)

  type TreatmentSummary with
    static member parse (x:tsProvider.Topic) =
      let build c t = Summary.from t |> TreatmentSummary.from c
      let buildIndex c t = Summary.fromIndex t |> TreatmentSummary.from c
      match x with
        | HasOutputClass "comparativeInformation" t -> t |> build ComparativeInformation
        | HasOutputClass "managementOfConditions" t -> t |> build ManagementOfConditions
        | HasOutputClass "medicalEmergenciesBodySystems" t -> t |> build MedicalEmergenciesBodySystems
        | HasOutputClass "treatmentOfBodySystems" t -> t |> build TreatmentOfBodySystems
        | HasOutputClass "about" t -> t |> build About
        | HasOutputClass "guidance" t -> t |> build Guidance
        | HasIndex ("index", "About") t -> t |> buildIndex AboutIndex
        | HasIndex ("index", "Guidance") t -> t |> buildIndex GuidanceIndex
        | t -> t |> build Generic


module DrugClassification =
  type dcProvider = XmlProvider<"../../samples/drugClassifications.xml", Global=true>

  type Classification = {key:string; value:string}

  type DrugClassifications = | DrugClassifications of Classification list

module DrugClassificationParser =
  open DrugClassification

  type Classification with
    static member from (x:dcProvider.Section) =
      let k = x.Ps.[0].Value
      let v = x.Ps.[1].Value
      {key = k; value = v}

  type DrugClassifications with
    static member parse (x:dcProvider.Topic) =
      x.Body.Sections |> Array.map Classification.from |> Array.toList |> DrugClassifications

module BorderlineSubstance =

  type bsProvider = XmlProvider<"../../samples/borderlinesubstances.xml", Global=true, SampleIsList=true>

  type Link = {Uri:string;Label:string;}

  type Category =
    | Category of string
    override __.ToString() = match __ with | Category x -> x

  type IntroductionNote =
    | IntroductionNote of string
    override __.ToString() = match __ with | IntroductionNote x -> x


  [<Measure>] type Kj
  [<Measure>] type Kcal
  [<Measure>] type g

  type Detail =
    | Formulation of string
    | EnergyKj of string
    | EnergyKcal of string
    | ProteinGrams of string
    | ProteinConstituents of string
    | CarbohydrateGrams of string
    | CarbohydrateConstituents of string
    | FatGrams of string
    | FatConstituents of string
    | FibreGrams of string
    | SpecialCharacteristics of string
    | Acbs of bsProvider.P
    | Presentation of bsProvider.P
    | PresentationNote of bsProvider.P
    | RxAdvice of string

  type Manufacturer =
    | Manufacturer of string
    override __.ToString() = match __ with | Manufacturer x -> string x

  type PreparationTitle = | PreparationTitle of bsProvider.P * Manufacturer option


  type PackSize =
    | PackSize of decimal
    override __.ToString() = match __ with | PackSize x -> string x

  type UnitOfMeasure =
    | UnitOfMeasure of string
    override __.ToString() = match __ with | UnitOfMeasure x -> string x

  type PackAcbs =
    | PackAcbs of string
    override __.ToString() = match __ with | PackAcbs x -> x

  type PackInfo = | PackInfo of PackSize option * UnitOfMeasure option * PackAcbs option


  type NhsIndicative =
    | NhsIndicative of string
    override __.ToString() = match __ with | NhsIndicative x -> x

  type PriceText =
    | PriceText of string
    override __.ToString() = match __ with | PriceText x -> x

  type NhsIndicativePrice =
    | NhsIndicativePrice of decimal
    override __.ToString() = match __ with | NhsIndicativePrice x -> string x

  type NhsIndicativeInfo = | NhsIndicativeInfo of NhsIndicative option * PriceText option * NhsIndicativePrice option


  type PackSizePriceTariff = | PackSizePriceTariff of PackInfo option * NhsIndicativeInfo option


  type BorderlineSubstancePrep = | BorderlineSubstancePrep of PreparationTitle option * PackSizePriceTariff list

  type Details = | Details of Detail list * BorderlineSubstancePrep list

  type BorderlineSubstance = {
    id:Id;
    title:bsProvider.Title;
    category:Category;
    intro:IntroductionNote option;
    details:Details list;
  }


module BorderlineSubstanceParser =
  open BorderlineSubstance

  type IntroductionNote with
    static member from (x:bsProvider.P) =
      match x with
        | HasOutputClasso "introductionNote" _ ->
          x.String >>= (IntroductionNote >> Some)
        | _ -> None

  let unit<[<Measure>]'u> = int >> LanguagePrimitives.Int32WithMeasure<'u>

  type Manufacturer with
    static member from (x:bsProvider.Ph) =
      match x with
        | HasOutputClass "manufacturer" ph ->
          ph.XElement.Remove()  //Side effects
          ph.String <!> (removebrackets >> Manufacturer)
        | _ -> None

  type PreparationTitle with
    static member from (x:bsProvider.P) =
      let m (p:bsProvider.P) = p.Phs |> Array.tryPick Manufacturer.from
      match x with
        | HasOutputClasso "title" p -> PreparationTitle(p, m p) |> Some
        | _ -> None

  let fromphn c (x:bsProvider.Ph) = x.Number <!> c
  let fromphs c (x:bsProvider.Ph) = x.String <!> c

  type UnitOfMeasure with
    static member from (x:bsProvider.Ph) = x.String <!> UnitOfMeasure

  type PackInfo with
    static member from (x:bsProvider.P) =
      let ps = x.Phs |> Array.tryPick (hasOutputclass "packSize") >>= (fromphn PackSize)
      let uom = x.Phs |> Array.tryPick (hasOutputclass "unitOfMeasure") >>= UnitOfMeasure.from
      let acbs = x.Phs |> Array.tryPick (hasOutputclass "acbs") >>= (fromphs (removebrackets >> PackAcbs))
      PackInfo(ps,uom,acbs)

  type NhsIndicativeInfo with
    static member from (x:bsProvider.P) =
      let nhsi = x.Phs |> Array.tryPick (hasOutputclass "nhsIndicative") >>= (fromphs NhsIndicative)
      let pt = x.Phs |> Array.tryPick (hasOutputclass "priceText") >>= (fromphs PriceText)
      let nhsip = x.Phs |> Array.tryPick (hasOutputclass "nhsIndicativePrice") >>= (fromphn NhsIndicativePrice)
      NhsIndicativeInfo(nhsi,pt,nhsip)

  type PackSizePriceTariff with
    static member from (x:bsProvider.Li) =
      let pi = x.Ps |> Array.tryPick (hasOutputclasso "packInfo") >>= (PackInfo.from >> Some)
      let nhs = x.Ps |> Array.tryPick (hasOutputclasso "nhsIndicativeInfo") >>= (NhsIndicativeInfo.from >> Some)
      PackSizePriceTariff(pi,nhs)

  type BorderlineSubstancePrep with
    static member from (x:bsProvider.Sectiondiv) =
      let title = x.P >>= PreparationTitle.from
      let pt = match x.Ul with
                | Some ul -> ul.Lis |> Array.map PackSizePriceTariff.from |> Array.toList
                | None -> []
      BorderlineSubstancePrep(title,pt)

  type Detail with
    static member from (x:bsProvider.P) =
      match x with
        | HasOutputClasso "formulation" p -> p.String <!> Formulation
        | HasOutputClasso "energyKj" p -> p.String <!> EnergyKj
        | HasOutputClasso "energyKcal" p -> p.String <!> EnergyKcal
        | HasOutputClasso "proteinGrams" p -> p.String <!> ProteinGrams
        | HasOutputClasso "proteinConstituents" p -> p.String <!> ProteinConstituents
        | HasOutputClasso "carbohydrateGrams" p -> p.String <!> CarbohydrateGrams
        | HasOutputClasso "carbohydrateConstituents" p -> p.String <!> CarbohydrateConstituents
        | HasOutputClasso "fatGrams" p -> p.String <!> FatGrams
        | HasOutputClasso "fatConstituents" p -> p.String <!> (removebrackets >> FatConstituents)
        | HasOutputClasso "fibreGrams" p -> p.String <!> FibreGrams
        | HasOutputClasso "specialCharacteristics" p -> p.String <!> SpecialCharacteristics
        | HasOutputClasso "acbs" p -> p |> (Acbs >> Some)
        | HasOutputClasso "presentation" p -> p |> Presentation |> Some
        | HasOutputClasso "presentationNote" p -> p |> PresentationNote |> Some
        | HasOutputClasso "rxAdvice" p -> p.String <!> RxAdvice
        | _ -> None

    static member from (x:bsProvider.Section) =
      let ds = match x with
               | HasOutputClass "details" s ->
                 s.Ps |> Array.choose Detail.from |> Array.toList
               | _ -> []
      let bsps = match x.Sectiondiv with
                  | Some sd ->  sd.Sectiondivs |> Array.map BorderlineSubstancePrep.from |> Array.toList
                  | None -> []
      Details(ds,bsps)


  type BorderlineSubstance with
    static member parse (x:bsProvider.Topic) =
      let c = x.Body.Xref.Href |> Category
      let note = x.Body.Ps |> Array.tryPick IntroductionNote.from
      let ds = x.Body.Sections |> Array.map Detail.from |> Array.toList

      {id = Id(x.Id)
       title = x.Title;
       category = c;
       intro = note;
       details = ds;
       }

module Interaction =
  type inProvider = XmlProvider<"../../samples/superinteraction.xml", Global=true, SampleIsList=true>

  type Link = {href: Href; label: string;}

  type NoteType = | Note

  type Note = | Note of inProvider.P * NoteType

  type Importance =
    | Mild
    | Moderate   
    | Severe
    | Unknown
    | NotSet
    override __.ToString() = toString __

  type Evidence =
    | Anecdotal
    | Study
    | Theoretical
    | NotSet
    override __.ToString() = toString __

  type Message = {importance:Importance; evidence:Evidence; pElem:inProvider.P}

  type InteractsWith =
    {id:Id;
     title:inProvider.Title;
     messages: Message list}

  type InteractionList =
    | InteractionList of Id * inProvider.Title * InteractsWith list 

  let defaultValueKey = "__default"  
    
  let allowedImportanceValues =
    ["mild", Mild
     "moderate", Moderate
     "severe", Severe
     "unknown", Unknown
     defaultValueKey, Importance.NotSet] |> Map.ofList 

  let allowedEvidenceValues = 
    ["anecdotal", Anecdotal
     "study", Study
     "theoretical", Theoretical
     defaultValueKey, Evidence.NotSet] |> Map.ofList

module InteracitonParser =
  open Interaction

  let getClassValueAs<'T> (allowedValues:Map<string, 'T>) (ph:inProvider.Ph option) =
    match ph with
        | Some s when allowedValues.ContainsKey(s.Class.Value) -> allowedValues.Item(s.Class.Value)
        | _ ->  allowedValues.Item(defaultValueKey)

  type InteractsWith with
    static member from (x:inProvider.Topic) =
      if x.Body.Ps.Length = 0 then failwith "cant find paragraph"
      
      let getPh outputClass (p:inProvider.P) = 
         p.Phs |> Seq.tryFind (fun x -> x.Outputclass = outputClass) 

      let removeNodeWhen conditionFun (p:inProvider.P) = 
        p.Phs |> Array.iter (fun ph -> if conditionFun ph then ph.XElement.Remove() else ())

      let createMessage (p:inProvider.P) =         
        let importance = p |> getPh "int-severity" |> getClassValueAs<Importance> allowedImportanceValues
        let evidence = p |> getPh "int-evidence" |> getClassValueAs<Evidence> allowedEvidenceValues

        removeNodeWhen (fun ph -> ph.Outputclass = "int-severity" || ph.Outputclass = "int-evidence") p

        {importance = importance; evidence = evidence; pElem = p} 

      let messages = 
        x.Body.Ps 
        |> Array.map createMessage         
        |> Array.toList

      {id=Id(x.Id); title=x.Title; messages = messages}

  type InteractionList with
    static member parse (x:inProvider.Topic) =

      let is = x.Topics |> Array.map InteractsWith.from |> Array.toList
      InteractionList(Id(x.Id),x.Title,is)


module WoundManagement =

  type wmProvider = XmlProvider<"../../samples/superwoundmanagment.xml", Global=true, SampleIsList=true>

  type TypeOfWound = | TypeOfWound of string

  type Description = | Description of wmProvider.Sectiondiv

  type WoundManagementLink = {id:Id;label:string;rel:string;}

  type WoundExudate = | WoundExudate of string * WoundManagementLink list

  type WoundType = | WoundType of TypeOfWound * Description option * WoundExudate list

  type Product = {
    ampid: int64;
    name: string;
    price: decimal;
    manufacturer: string;
  }

  type ProductGroup = | ProductGroup of wmProvider.P * Description option * Product list

  type WoundManagement = {
    id: Id;
    title: wmProvider.Title;
    general: wmProvider.Section option;
    dressingChoices: WoundType list;
    links: WoundManagementLink list;
    //products: Product list; //need to check if they exist in isolation
    productGroups: ProductGroup list
  }

module WoundManagementParser =
  open WoundManagement

  let desc = Array.tryPick (hasOutputclasso "description" >> Option.map Description)

  type WoundManagementLink with
    static member from (x:wmProvider.Xref) =
      {WoundManagementLink.id=Id(x.Href);label=x.Value;rel=x.Rel |? ""}

  type WoundExudate with
    static member list (x:wmProvider.Sectiondiv[]) =
      x |> Array.choose (hasOutputclasso "woundExudate" >> Option.map WoundExudate.from) |> Array.toList
    static member from (x:wmProvider.Sectiondiv) =
      let r = x.Ps.[0].Value.Value
      let ls = x.Xrefs |> Array.map WoundManagementLink.from |> Array.toList
      WoundExudate(r,ls)

  type TypeOfWound with
    static member from (x:wmProvider.P) = x.Value <!> TypeOfWound

  type WoundType with
    static member list (x:wmProvider.Section) =
      x.Sectiondivs |> Array.choose (hasOutputclasso "woundType" >> Option.map WoundType.from)
    static member from (x:wmProvider.Sectiondiv) =
      let tow = x.Ps |> Array.pick (hasOutputclasso "typeOfWound" >> Option.bind TypeOfWound.from)
      let d = x.Sectiondivs |> desc
      let wes = x.Sectiondivs |> WoundExudate.list
      WoundType(tow,d,wes)

  type Product with
    static member from (x:wmProvider.P) =
      let n = x.Phs |> Array.tryPick (hasOutputclass "name")
      let p = x.Phs |> Array.tryPick (hasOutputclass "price")
      let m = x.Phs |> Array.tryPick (hasOutputclass "manufacturer")
      match (x.Data,n,p,m) with
        | (Some d, Some name, Some price, Some man) ->
           {ampid=d.Value;name=name.String.Value;price=price.Number.Value;manufacturer=man.String.Value}
        | _ -> failwith "missing part of the product"

    static member from (x:wmProvider.Sectiondiv) =
      x.Ps |> Array.tryPick (hasOutputclasso "product") <!> Product.from

    static member list (x:wmProvider.Sectiondiv[]) =
      x |> Array.choose Product.from |> Array.toList

  type ProductGroup with
    static member list (x:wmProvider.Section) =
      x.Sectiondivs |> Array.choose (hasOutputclasso "productGroup" >> Option.map ProductGroup.from)
    static member from (x:wmProvider.Sectiondiv) =
      let t = x.Ps |> Array.pick (hasOutputclasso "title")
      let d = x.Sectiondivs |> desc
      let ps = x.Sectiondivs |> Product.list
      ProductGroup(t,d,ps)

  type WoundManagement with
    static member parse (x:wmProvider.Topic) =
      let gen = x.Body.Sections |> Array.tryPick (hasOutputclass "general")
      let dcs = x.Body.Sections
                |> Array.choose (hasOutputclass "dressingChoices")
                |> Array.collect WoundType.list
                |> Array.toList
      let ls = x.Xrefs |> Array.map WoundManagementLink.from |> Array.toList
      let pgs = x.Body.Sections
                |> Array.choose (hasOutputclass "productGroups")
                |> Array.collect ProductGroup.list
                |> Array.toList
      {id=Id(x.Id);title=x.Title;general=gen;dressingChoices=dcs;links=ls;productGroups=pgs}



module Generic =
  type genericProvider = XmlProvider<"../../samples/supercontent.xml", Global=true, SampleIsList=true>

  type TargetAudience = | TargetAudience of string
  type Content = | Content of genericProvider.Section * TargetAudience option

  type Generic = {
    id:Id;
    title:genericProvider.Title;
    content:Content list;
    links:ContentLink seq
  }

module GenericParser =
  open Generic

  type Content with
    static member from (x:genericProvider.Section) =
      Content(x,x.Outputclass <!> TargetAudience)

  type Generic with
    static member parse (x:genericProvider.Topic) =
      let c = match x.Body with
              | Some b -> b.Sections |> Array.map Content.from |> Array.toList
              | None -> []
      let ls = x.XElement |> ContentLink.from
      {id=Id(x.Id); title = x.Title; content = c; links = ls}

module Index =
  type indexProvider = XmlProvider<"../../samples/medicalDevices.xml", Global=true>

  type Index = | Index of Id * Id list

module IndexParser =
  open Index
  type Index with
    static member parse (x:indexProvider.Topic) =
      let ids = x.Xrefs |> Array.map (fun xref -> xref.Href |> Id) |> Array.toList
      Index(Id(x.Id),ids)

module PublicationInfo =
  open Shared
  type publicationInfoProvider = XmlProvider<"../../samples/PHP001.xml", Global=true>
  type PublicationInfo =
    | PublicationInfo of System.DateTime

module PublicationInfoParser =
  open Shared
  open PublicationInfo
  type PublicationInfo with
    static member parse (x:publicationInfoProvider.Topic) =
      PublicationInfo(x.Prolog.Critdates.Revised.Modified)

module BorderlineSubstanceTaxonomy =


  type BorderlineSubstanceTaxonomy = {
    id:Id;
    title: drugProvider.Title;
    general: drugProvider.Section option
    substances: Id list;
    categories: Id list;
    acbs: drugProvider.P option
    }


module BorderlineSubstanceTaxonomyParser =
  open BorderlineSubstanceTaxonomy

  type BorderlineSubstanceTaxonomy with
    static member parse (x:drugProvider.Topic) =
      let general = x |> sections "general" |> Array.tryPick Some
      let ids = match x.Body with
                   | Some b -> b.Sections
                                |> Array.collect (fun s -> s.Ps)
                                |> Array.collect (fun p -> p.Xrefs)
                                |> Array.map (fun x -> x.Href |> Id)
                                |> Array.toList
                   | None -> []
      let cats = x.Xrefs |> Array.map (fun x -> x.Href |> Id) |> Array.toList
      let acbs = x |> (somesectiondivs "acbsIndications")
                   |> Array.tryPick (fun sd -> match sd.Ps with
                                               | [|p|] -> Some p
                                               | _ -> None)
      {id=Id(x.Id);title=x.Title;general=general;substances=ids;categories=cats; acbs = acbs}

module MedicalDevice =

  type PrescribingAndDispensingInformation =
    | PrescribingAndDispensingInformation of drugProvider.Title option * drugProvider.Sectiondiv

  type MedicalDevice =
    | MedicalDevice of Id * drugProvider.Title * PrescribingAndDispensingInformation option * Id list


module MedicalDeviceParser =
  open MedicalDevice

  type MedicalDevice with

    static member parse (x:drugProvider.Topic) =
      let s = x |> topics "prescribingAndDispensingInformation"
                |> Array.collect allsectiondivs
                |> Array.tryPick Some

      let t = x |> topics "prescribingAndDispensingInformation"
                |> Array.map (fun t -> t.Title)
                |> Array.tryPick Some

      let padi = s <!> (fun s' -> PrescribingAndDispensingInformation(t,s'))

      let id (x:drugProvider.Xref) = x.Href |> Id
      let links (x:drugProvider.Topic) = x.Xrefs |> Array.map id
      let ids = x |> topics "medicalDeviceTypes"
                  |> Array.collect links
                  |> Array.toList
      MedicalDevice(Id(x.Id),x.Title,padi,ids)


module Sections =

  type sectionProvider = XmlProvider<"../../samples/others.xml", Global=true, SampleIsList=true>

  type Title =
    //| TextTitle of string
    | XmlTitle of sectionProvider.P
    override x.ToString() =
      failwith "should not call string on this"
      ""
     

  type NormalPlasmaValues = {
    title:Title;
    sodium:string;
    potassium:string;
    bicarbonate:string;
    chloride:string;
    calcium:string;
    }

  type IntravenousInfusion = {
    title:Title;
    sodium:string option;
    potassium:string option;
    bicarbonate:string option;
    chloride:string option;
    calcium:string option;
    forMetabolicAcidosis:bool option
    }


  type ElectrolyteConcentrations =
    | ElectrolyteConcentrations of Title * NormalPlasmaValues * IntravenousInfusion list

  type TypeOfFluid = {
    title:Title;
    hydrogen:string option;
    sodium:string option;
    potassium:string option;
    bicarbonate:string option;
    chloride:string option;
    }

  type ElectrolyteContent = | ElectrolyteContent of Title * TypeOfFluid list


  let p oc = (hasOutputclasso oc) >> Option.bind (fun (p:sectionProvider.P) -> p.String)

  let title = hasOutputclasso "title" >> Option.map XmlTitle

  let rec unravel (ocs:string list) (x:sectionProvider.Sectiondiv) =
    match ocs with
      | [] -> [x]
      | head :: tail ->
        x.Sectiondivs
          |> Array.choose (hasOutputclass head)
          |> Array.toList
          |> List.collect (unravel tail)

  let unravelr (ocs: string list) (x:sectionProvider.Section) =
    match ocs with
      | [] -> failwith "must have oc"
      | head::tail ->
        x.Sectiondivs
          |> Array.choose (hasOutputclass head)
          |> Array.toList
          |> List.collect (unravel tail)

  type FluidAndElectrolytes = {
    id:Id
    title:Title option
    concentrations:ElectrolyteConcentrations
    content:ElectrolyteContent
    } with

    static member parse (x:sectionProvider.Section) =
      let concentrations (x:sectionProvider.Sectiondiv) =
        let normalplasmavalues (x:sectionProvider.Sectiondiv) = {
            NormalPlasmaValues.title = x.Ps |> Array.pick title
            sodium = x.Ps |> Array.pick (p "sodium")
            potassium = x.Ps |> Array.pick (p "potassium")
            bicarbonate = x.Ps |> Array.pick (p "bicarbonate")
            chloride = x.Ps |> Array.pick (p "chloride")
            calcium = x.Ps |> Array.pick (p "calcium")
          }

        let intravenousinfusion (x:sectionProvider.Sectiondiv) = {
            IntravenousInfusion.title = x.Ps |> Array.pick title
            sodium = x.Ps |> Array.tryPick (p "sodium")
            potassium = x.Ps |> Array.tryPick (p "potassium")
            bicarbonate = x.Ps |> Array.tryPick (p "bicarbonate")
            chloride = x.Ps |> Array.tryPick (p "chloride")
            calcium = x.Ps |> Array.tryPick (p "calcium")
            forMetabolicAcidosis = x.Data |> Option.map (fun d -> d.Value)
          }

        let title = x.Ps |> Array.pick title
        let npv = x.Sectiondivs
                    |> Array.pick ((hasOutputclass "normalPlasmaValues") >> (Option.map normalplasmavalues))
        let iis = x.Sectiondivs
                    |> Array.choose (hasOutputclass "intravenousInfusions")
                    |> Array.collect (fun s -> s.Sectiondivs |> Array.map intravenousinfusion)
                    |> Array.toList
        ElectrolyteConcentrations(title,npv,iis)

      let electrolytecontent (x:sectionProvider.Sectiondiv) =
        let typeoffluid (x:sectionProvider.Sectiondiv) = {
            TypeOfFluid.title = x.Ps |> Array.pick title
            sodium = x.Ps |> Array.tryPick (p "sodium")
            potassium = x.Ps |> Array.tryPick (p "potassium")
            bicarbonate = x.Ps |> Array.tryPick (p "bicarbonate")
            chloride = x.Ps |> Array.tryPick (p "chloride")
            hydrogen = x.Ps |> Array.tryPick (p "hydrogen")
          }

        let title = x.Ps |> Array.pick title
        let ftypes = x.Sectiondivs
                        |> Array.choose (hasOutputclass "typesOfFluid")
                        |> Array.collect (fun s -> s.Sectiondivs |> Array.map typeoffluid)
                        |> Array.toList
        ElectrolyteContent(title,ftypes)


      let title = x.Ps |> Array.tryPick title
      let econcen = x.Sectiondivs |> Array.pick (hasOutputclass "electrolyteConcentrations" >> Option.map concentrations)
      let content = x.Sectiondivs |> Array.pick (hasOutputclass "electrolyteContent" >> Option.map electrolytecontent)
      {id = Id(x.Id); title = title; concentrations = econcen; content = content}



  type Pack = | Pack of sectionProvider.P

  type Preparation = {
    title:Title;
    manufacturer:string;
    nitrogen:string option;
    energy:string option;
    potassium:string option;
    magnesium:string option;
    sodium:string option;
    acetate:string option;
    chloride:string option;
    otherComponentsPerLitre:sectionProvider.P option;
    adultOnly:bool option;
    packs:Pack list;
    }

  type EnergyNotes = | EnergyNotes of sectionProvider.Sectiondiv

  open Option

  type ParenteralFeeding =
    | ParenteralFeeding of Id * EnergyNotes * Preparation list
    static member parse (x:sectionProvider.Section) =
      let preparation (x:sectionProvider.Sectiondiv) =
        let title (p:sectionProvider.P) =
          let ti = p |> XmlTitle |> Some
          let man = p.Phs
                    |> Array.tryPick ((hasOutputclass "manufacturer") >> Option.bind (fun ph ->
                                                                              ph.XElement.Remove() //side effects
                                                                              ph.String) >> (Option.map removebrackets))
          Option.lift2 (fun a b -> (a,b)) ti man
        let pack (x:sectionProvider.Sectiondiv) =
          match x.Ps with
          | [|p|] -> Pack p
          | _ -> failwith "no paragraph found"

        let ti,man = x.Ps |> Array.pick title
        {
          Preparation.title = ti
          manufacturer = man
          nitrogen = x.Ps |> Array.tryPick (p "nitrogen")
          energy = x.Ps |> Array.tryPick (p "energy")
          potassium = x.Ps |> Array.tryPick (p "potassium")
          magnesium = x.Ps |> Array.tryPick (p "magnesium")
          sodium = x.Ps |> Array.tryPick (p "sodium")
          acetate = x.Ps |> Array.tryPick (p "acetate")
          chloride = x.Ps |> Array.tryPick (p "chloride")
          otherComponentsPerLitre = x.Ps |> Array.tryPick (hasOutputclasso "otherComponentsPerLitre")
          adultOnly = x.Data |> Option.map (fun d -> d.Value)
          packs = x.Sectiondivs
                    |> Array.choose (hasOutputclass "packs")
                    |> Array.collect (fun ps -> ps.Sectiondivs |> Array.map pack)
                    |> Array.toList
          }
      let preps = x.Sectiondivs
                    |> Array.choose (hasOutputclass "preparations")
                    |> Array.collect (fun sd -> sd.Sectiondivs |> Array.map preparation)
                    |> Array.toList

      let en = x.Sectiondivs
                |> Array.pick (hasOutputclass "energyNotes" >> Option.map EnergyNotes)

      ParenteralFeeding(Id(x.Id),en,preps)



  type IncedenceDuration =
    | IncedenceDuration of string
    override __.ToString() = match __ with | IncedenceDuration s -> s

  type Incedence = | Incedence of IncedenceDuration * string

  type IncedencesType =
   | BackgroundIncidences
   | AdditionalCasesOestrogenOnly
   | AdditionalCasesCombined

  type Incedences = | Incedences of Title option * IncedencesType * Incedence list

  type AgeRange =
    | AgeRange of string
    override __.ToString() = match __ with | AgeRange s -> s

  type Group = | Group of AgeRange * Incedences list

  type Note = | Note of sectionProvider.Sectiondiv

  type Risk = | Risk of Title * Note option * Group list

  type HrtRisks =
    | HrtRisks of Id * Note * Risk list

    static member parse (x:sectionProvider.Section) =
      let risk (x:sectionProvider.Sectiondiv) =
        let group (x:sectionProvider.Sectiondiv) =
          let incedences (x:sectionProvider.Sectiondiv) =
            let incidence (x:sectionProvider.Sectiondiv) =
              let duration = x.Ps |> Array.pick (p "incidenceDuration" >> Option.map IncedenceDuration)
              let count = x.Ps |> Array.pick (p "incidence")
              Incedence(duration,count)
            let itype = match x.Outputclass with
                        | "backgroundIncidences" -> BackgroundIncidences
                        | "additionalCasesOestrogenOnly" -> AdditionalCasesOestrogenOnly
                        | "additionalCasesCombined" -> AdditionalCasesCombined
                        | _ -> failwith "type not matched"
            let title = x.Ps |> Array.tryPick title
            let incedences = x.Sectiondivs
                             |> Array.collect (fun s -> s.Sectiondivs)
                             |> Array.map incidence
                             |> Array.toList
            Incedences(title,itype,incedences)
          let ar = x.Ps |> Array.pick (p "ageRange" >> Option.map AgeRange)
          let is = x.Sectiondivs |> Array.map incedences |> Array.toList
          Group(ar,is)

        let title = x.Ps |> Array.pick title
        let note = x |> unravel ["notes"] |> List.tryPick (Note >> Some)
        let groups = x |> unravel ["groups";"group"] |> List.map group
        Risk(title,note,groups)

      let note = x |> unravelr ["note"] |> List.pick (Note >> Some)
      let risks = x |> unravelr ["risks";"risk"] |> List.map risk
      HrtRisks(Id(x.Id),note,risks)

  type CompatibleStrip = {
    name: Title;
    packSize: string;
    price: decimal
    }

  type BloodMonitoringStrip = {
    meter : sectionProvider.P
    typeOfMonitoring: string
    compatibleStrip : CompatibleStrip
    sensitivityRange: sectionProvider.P
    manufacturer: string
    note: string option
    }

  type BloodMonitoringStrips =
    | BloodMonitoringStrips of Id * Title option * BloodMonitoringStrip list
    static member parse (x:sectionProvider.Section) =
      let strip (x:sectionProvider.Sectiondiv) =
        let compatiblestrip (x:sectionProvider.Sectiondiv) =
          let pi (x:sectionProvider.Sectiondiv) =
            match x.Ps with
            | [|p|] -> match p.Phs with
                       | [|ps;pr|] -> Option.lift2 (fun a b -> (a,b)) ps.String pr.Number
                       | _ -> None
            | _ -> None
          let name = x.Ps |> Array.pick (hasOutputclasso "compatibleStripName" >> Option.map XmlTitle)
          let psize,price = x |> unravel ["compatibleStrips"] |> List.pick pi
          {
            name = name
            packSize = psize
            price = price
            }

        {
        meter = x.Ps |> Array.pick (hasOutputclasso "meter")
        typeOfMonitoring = x.Ps |> Array.pick (p "typeOfMonitoring")
        compatibleStrip = x |> compatiblestrip
        sensitivityRange = x.Ps |> Array.pick (hasOutputclasso "sensitivityRange")
        manufacturer = x.Ps |> Array.pick (p "manufacturer")
        note = x.Ps |> Array.tryPick (p "note")
        }
      let title = x.Ps |> Array.tryPick title
      BloodMonitoringStrips(Id(x.Id),title, x |> unravelr ["bloodMonitoringStrip"] |> List.map strip)


  type TherapyType = | Combination | Single
  type Supervision = | Supervised | Unsupervised
  type AgeGroup =
    | Adult of sectionProvider.P
    | Child of sectionProvider.P

  type Dosage = | Dosage of sectionProvider.P

  type PatientGroup = | PatientGroup of AgeGroup * Dosage option
  type TakenInMonths =
    | TakenInMonths of string
    override __.ToString() = match __ with | TakenInMonths s -> s

  type Therapy = {
    therapyType:TherapyType;
    supervision:Supervision;
    title:Title;
    takenInMonths:TakenInMonths;
    groups:PatientGroup list;
  }

  type TherapyNotes = {
    supervision:Supervision;
    notes: sectionProvider.Sectiondiv
  }

  type AntiTuberculosisTreatments =
    | AntiTuberculosisTreatments of Id * Title option * Therapy list * TherapyNotes list
    static member parse (x:sectionProvider.Section) =
      let therapyNotes su (x:sectionProvider.Sectiondiv) =
        {
          supervision = su
          notes = x
        }
      let therapy ty su (x:sectionProvider.Sectiondiv) =
        let takeninmonths (x:sectionProvider.P) =
          x.Phs |> Array.tryPick (hasOutputclass "takenInMonths" >> Option.bind (fun ph -> ph.String) >> Option.map TakenInMonths)
        let group (x:sectionProvider.Li) =
          match x.Outputclass,x.Ps with
            | "patientGroup adult",[|p;r|] ->
                (p |> Adult, r |> Dosage |> Some) |> PatientGroup |> Some
            | "patientGroup child",[|p;r|] ->
                (p |> Child, r |> Dosage |> Some) |> PatientGroup |> Some
            | _ -> None

        let title = x.Ps |> Array.pick title
        let takenInMonths = x.Ps |> Array.pick (hasOutputclasso "title" >> Option.bind takeninmonths)
        let groups = x.Ul |> function
                              | Some ul -> ul.Lis |> Array.choose group |> Array.toList
                              | None -> []
        {
          therapyType = ty
          supervision = su
          title = title
          takenInMonths = takenInMonths
          groups = groups
          }
      let title = x.Ps |> Array.tryPick title

      let a = x |> unravelr ["unsupervisedTreatment";"combinationDrugTherapies";"combinationDrugTherapy"]
                |> List.map (therapy Combination Unsupervised)
      let b = x |> unravelr ["unsupervisedTreatment";"singleDrugTherapies";"singleDrugTherapy"]
                |> List.map (therapy Single Unsupervised)

      let c = x |> unravelr ["supervisedTreatment";"combinationDrugTherapies";"combinationDrugTherapy"]
                |> List.map (therapy Combination Supervised)
      let d = x |> unravelr ["supervisedTreatment";"singleDrugTherapies";"singleDrugTherapy"]
                |> List.map (therapy Single Supervised)

      let e = x |> unravelr ["unsupervisedTreatment";"drugTherapyNotes";"drugTherapyNote"]
                |> List.map (therapyNotes Unsupervised)

      let f = x |> unravelr ["supervisedTreatment";"drugTherapyNotes";"drugTherapyNote"]
                |> List.map (therapyNotes Supervised)

      AntiTuberculosisTreatments(Id(x.Id),title,a @ b @ c @ d,e @ f)


  type Quantity = | Quantity of string

  type Drug =
    | AcidSuppressant of string * Quantity
    | Antibacterial of string * Quantity

  type Course = | Course of sectionProvider.P

  type Regimen = | Regimen of Title * Drug list * Course option * PatientGroup option

  type HelicobacterPyloriRegimens =
    | HelicobacterPyloriRegimens of Id * Title option * Regimen list
    static member parse (x:sectionProvider.Section) =
      let regimen (x:sectionProvider.Sectiondiv * PatientGroup option) =
        let sec, ti = x
        let drug (x:sectionProvider.Sectiondiv) =
          match x.Outputclass,x.Ps with
            | "acidSuppressant",[|d;q|]
               -> Option.lift2 (fun d q -> AcidSuppressant(d,Quantity(q))) d.String q.String
            | "antibacterial",[|d;q|]
               -> Option.lift2 (fun d q -> Antibacterial(d,Quantity(q))) d.String q.String
            | (_,_) -> failwith "unknown class"

        let title = sec.Ps |> Array.pick title
        let course = sec.Ps |> Array.tryPick(hasOutputclasso "course" >> Option.map Course)
        let acid = sec |> unravel ["acidSuppressant"] |> List.choose drug
        let anti = sec |> unravel ["antibacterials";"antibacterial"] |> List.choose drug
        Regimen(title,acid @ anti,course, ti)
      
      let bnfcPatientGroups(x:sectionProvider.Sectiondiv) =  
        let count = x.Sectiondivs.Length
        let pg = match x.Outputclass,x.Ps with
                 | "patientGroup",[|p|] -> (p |> Child, None) |> PatientGroup |> Some
                 | (_,_) -> None
        Array.init count (fun _ -> pg) |> Array.toList

      let rsTempOriginal = x |> unravelr["regimens";"regimen"] 
      let emptyTitles = match rsTempOriginal with
                        | [] -> []
                        | _ -> [ for _ in 0..(rsTempOriginal.Length - 1) -> None]

      let rsTemp = List.zip rsTempOriginal emptyTitles  |> List.map regimen
      let rs = match rsTemp with
               | [] -> let titles = x |> unravelr["regimens";"patientGroup"] |> List.collect bnfcPatientGroups
                       let regimens = x |> unravelr["regimens";"patientGroup";"regimen"]
                       List.zip regimens titles |> List.map regimen
               | _ -> rsTemp
      let title = x.Ps |> Array.tryPick title
      HelicobacterPyloriRegimens(Id(x.Id),title,rs)


  type MalariaRisk = {
    content:string
    regimen:string
    }

  type Country =
    | Country of string
    override __.ToString() = match __ with | Country s -> s

  type MalariaProphylaxisRegimen = | MalariaProphylaxisRegimen of Country * MalariaRisk list

  type MalariaProphylaxisRegimens =
    | MalariaProphylaxisRegimens of Id * Title option * MalariaProphylaxisRegimen list
    static member parse (x:sectionProvider.Section) =
      let regimen (x:sectionProvider.Sectiondiv) =
        let risks (x:sectionProvider.Sectiondiv) =
          x |> unravel ["risks";"risk"]
            |> List.choose (fun sd -> match sd.Ps with
                                       | [|c;r|] -> Option.lift2 (fun c r -> {content = c;regimen = r}) c.String r.String
                                       | _ -> None)
        let country = x.Ps |> Array.pick (p "country" >> Option.map Country)
        let rsks = x |> risks
        MalariaProphylaxisRegimen(country,rsks)

      let title = x.Ps |> Array.tryPick title
      let regs = x |> unravelr ["malariaProphylaxisRegimen"] |> List.map regimen
      MalariaProphylaxisRegimens(Id(x.Id),title,regs)

  type DoseStatement = {
    age:string
    dose:string
    volume:sectionProvider.P
    note:string option
    }

  type IntramuscularAdrenalineEmergency = {
    id:Id
    title:Title option
    note:string option
    statements:DoseStatement list
    }
    with

    static member parse (x:sectionProvider.Section) =

      let statement (x:sectionProvider.Sectiondiv) =
        {
          age = x.Ps |> Array.pick (p "age")
          dose = x.Ps |> Array.pick (p "dose")
          volume = x.Ps |> Array.pick (hasOutputclasso "volume")
          note = x.Ps |> Array.tryPick (p "note")
          }
      {
        id = Id(x.Id)
        title = x.Ps |> Array.tryPick title
        note = x.Ps |> Array.tryPick (p "note")
        statements = x |> unravelr ["doses";"doseStatement"] |> List.map statement
        }
