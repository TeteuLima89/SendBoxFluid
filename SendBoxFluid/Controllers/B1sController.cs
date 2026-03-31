using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Models.Login;

namespace SendBoxFluid.Controllers;

[ApiController]
[Route("b1s")]
public class B1sController : ControllerBase
{
    private readonly ILogger<B1sController> _logger;

    public B1sController(ILogger<B1sController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [Route("v1/Login")]
    public async Task<LoginResponse> Post(LoginRequest request)
    {
        _logger.LogInformation("=== LOGIN RECEBIDO ===");
        _logger.LogInformation("CompanyDB: {CompanyDB}, UserName: {UserName}", request.CompanyDB, request.UserName);
        _logger.LogInformation("Headers: {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={h.Value}")));
        _logger.LogInformation("RemoteIP: {IP}", HttpContext.Connection.RemoteIpAddress);

        var response = new LoginResponse
        {
            SessionId = Guid.NewGuid().ToString(),
            SessionTimeout = 30,
            Version = "1000300"
        };

        _logger.LogInformation("Resposta: SessionId={SessionId}", response.SessionId);
        return response;
    }

    [HttpPost]
    [Route("v1/Drafts")]
    public async Task<DraftsResponse> PostDrafts(DraftsRequest request)
    {
        return new DraftsResponse();
    }


    public class DraftsRequest
    {
        public string BPL_IDAssignedToInvoice { get; set; }

        public string CardCode { get; set; }

        public string Comments { get; set; }

        public string DocCurrency { get; set; }

        public string DocDate { get; set; }

        public string DocDueDate { get; set; }

        public string DocObjectCode { get; set; }

        public double DocRate { get; set; }

        public List<object> DocumentAdditionalExpenses { get; set; }

        public List<object> DocumentLines { get; set; }

        public List<object> DownPaymentsToDraw { get; set; }

        public string OpeningRemarks { get; set; }

        public string Reference2 { get; set; }

        public string SequenceCode { get; set; }

        public string SequenceModel { get; set; }

        public string SequenceSerial { get; set; }

        public string SeriesString { get; set; }

        public string TaxDate { get; set; }

        public Dictionary<string, object> TaxExtension { get; set; }

        public string TransportationCode { get; set; }

        public string U_ACT_ComexId { get; set; }

        public string U_ACT_DespType { get; set; }

        public string U_ACT_IntegrationLog { get; set; }

        public int U_ACT_NfeId { get; set; }

        public string U_ChaveAcesso { get; set; }
    }

    public class DraftsResponse
    {
        public object? ATDocumentType { get; set; }

        public object? AddLegIn { get; set; }

        public string Address { get; set; }

        public string Address2 { get; set; }

        public Dictionary<string, object> AddressExtension { get; set; }

        public object? AddressForReturn { get; set; }

        public object? AgentCode { get; set; }

        public object? AnnualInvoiceDeclarationReference { get; set; }

        public string ApplyCurrentVATRatesForDownPaymentsToDraw { get; set; }

        public string ApplyTaxOnFirstInstallment { get; set; }

        public string ArchiveNonremovableSalesQuotation { get; set; }

        public string AssetValueDate { get; set; }

        public object? AttachmentEntry { get; set; }

        public object? AuthorizationCode { get; set; }

        public string AuthorizationStatus { get; set; }

        public object? BPChannelCode { get; set; }

        public object? BPChannelContact { get; set; }

        public string BPLName { get; set; }

        public int BPL_IDAssignedToInvoice { get; set; }

        public int BaseAmount { get; set; }

        public int BaseAmountFC { get; set; }

        public int BaseAmountSC { get; set; }

        public string BillOfExchangeReserved { get; set; }

        public object? BlanketAgreementNumber { get; set; }

        public string BlockDunning { get; set; }

        public object? Box1099 { get; set; }

        public object? CancelDate { get; set; }

        public string CancelStatus { get; set; }

        public string Cancelled { get; set; }

        public string CardCode { get; set; }

        public string CardName { get; set; }

        public int CashDiscountDateOffset { get; set; }

        public object? CentralBankIndicator { get; set; }

        public object? CertificationNumber { get; set; }

        public object? Cig { get; set; }

        public object? ClosingDate { get; set; }

        public string ClosingOption { get; set; }

        public object? ClosingRemarks { get; set; }

        public string Comments { get; set; }

        public string CommissionTrade { get; set; }

        public string CommissionTradeReturn { get; set; }

        public string Confirmed { get; set; }

        public int ContactPersonCode { get; set; }

        public object? ControlAccount { get; set; }

        public string CreateOnlineQuotation { get; set; }

        public object? CreateQRCodeFrom { get; set; }

        public string CreationDate { get; set; }

        public object? Cup { get; set; }

        public object? CustOffice { get; set; }

        public object? DANFELgTxt { get; set; }

        public int DataVersion { get; set; }

        public object? DateOfReportingControlStatementVAT { get; set; }

        public string DeferredTax { get; set; }

        public int DiscountPercent { get; set; }

        public string DocCurrency { get; set; }

        public string DocDate { get; set; }

        public string DocDueDate { get; set; }

        public int DocEntry { get; set; }

        public int DocNum { get; set; }

        public string DocObjectCode { get; set; }

        public double DocRate { get; set; }

        public string DocTime { get; set; }

        public double DocTotal { get; set; }

        public double DocTotalFc { get; set; }

        public double DocTotalSys { get; set; }

        public string DocType { get; set; }

        public List<object> DocumentAdditionalExpenses { get; set; }

        public List<object> DocumentAdditionalIntrastatExpenses { get; set; }

        public string DocumentDelivery { get; set; }

        public List<object> DocumentDistributedExpenses { get; set; }

        public List<object> DocumentInstallments { get; set; }

        public List<object> DocumentLines { get; set; }

        public List<object> DocumentPackages { get; set; }

        public List<object> DocumentReferences { get; set; }

        public List<object> DocumentSpecialLines { get; set; }

        public string DocumentStatus { get; set; }

        public string DocumentSubType { get; set; }

        public object? DocumentTaxID { get; set; }

        public List<object> Document_ApprovalRequests { get; set; }

        public object? DocumentsOwner { get; set; }

        public int DownPayment { get; set; }

        public int DownPaymentAmount { get; set; }

        public int DownPaymentAmountFC { get; set; }

        public int DownPaymentAmountSC { get; set; }

        public int DownPaymentPercentage { get; set; }

        public string DownPaymentStatus { get; set; }

        public object? DownPaymentTrasactionID { get; set; }

        public string DownPaymentType { get; set; }

        public List<object> DownPaymentsToDraw { get; set; }

        public object? ECommerceGSTIN { get; set; }

        public object? ECommerceOperator { get; set; }

        public Dictionary<string, object> EDeliveryInfo { get; set; }

        public object? EDocErrorCode { get; set; }

        public object? EDocErrorMessage { get; set; }

        public object? EDocExportFormat { get; set; }

        public string EDocGenerationType { get; set; }

        public object? EDocNum { get; set; }

        public object? EDocSeries { get; set; }

        public string EDocStatus { get; set; }

        public string EDocType { get; set; }

        public object? ETaxNumber { get; set; }

        public object? ETaxWebSite { get; set; }

        public Dictionary<string, object> EWayBillDetails { get; set; }

        public object? ElecCommMessage { get; set; }

        public object? ElecCommStatus { get; set; }

        public List<object> ElectronicProtocols { get; set; }

        public string EndAt { get; set; }

        public object? EndDeliveryDate { get; set; }

        public object? EndDeliveryTime { get; set; }

        public string ExcludeFromTaxReportControlStatementVAT { get; set; }

        public object? ExemptionValidityDateFrom { get; set; }

        public object? ExemptionValidityDateTo { get; set; }

        public object? ExternalCorrectedDocNum { get; set; }

        public int ExtraDays { get; set; }

        public int ExtraMonth { get; set; }

        public string FCEAsPaymentMeans { get; set; }

        public object? FCI { get; set; }

        public string FatherCard { get; set; }

        public string FatherType { get; set; }

        public object? FederalTaxID { get; set; }

        public int FinancialPeriod { get; set; }

        public object? FiscalDocNum { get; set; }

        public object? FolioNumber { get; set; }

        public object? FolioNumberFrom { get; set; }

        public object? FolioNumberTo { get; set; }

        public object? FolioPrefixString { get; set; }

        public object? Form1099 { get; set; }

        public object? GSTTransactionType { get; set; }

        public object? GTSChecker { get; set; }

        public object? GTSPayee { get; set; }

        public string GroupHandWritten { get; set; }

        public object? GroupNumber { get; set; }

        public object? GroupSeries { get; set; }

        public string HandWritten { get; set; }

        public object? ImportFileNum { get; set; }

        public object? Indicator { get; set; }

        public string InsuranceOperation347 { get; set; }

        public string InterimType { get; set; }

        public object? InternalCorrectedDocNum { get; set; }

        public string InventoryStatus { get; set; }

        public string InvoicePayment { get; set; }

        public string IsAlteration { get; set; }

        public string IsPayToBank { get; set; }

        public int IssuingReason { get; set; }

        public string JournalMemo { get; set; }

        public int LanguageCode { get; set; }

        public object? LastPageFolioNumber { get; set; }

        public object? LegTextF { get; set; }

        public object? Letter { get; set; }

        public object? ManualNumber { get; set; }

        public string MaximumCashDiscount { get; set; }

        public string NTSApproved { get; set; }

        public object? NTSApprovedNumber { get; set; }

        public string NetProcedure { get; set; }

        public object? NextCorrectingDocument { get; set; }

        public string NotRelevantForMonthlyInvoice { get; set; }

        public object? NumAtCard { get; set; }

        public int NumberOfInstallments { get; set; }

        public string OpenForLandedCosts { get; set; }

        public string OpeningRemarks { get; set; }

        public object? OriginalCreditOrDebitDate { get; set; }

        public object? OriginalCreditOrDebitNo { get; set; }

        public object? OriginalRefDate { get; set; }

        public object? OriginalRefNo { get; set; }

        public object? POSCashierNumber { get; set; }

        public object? POSDailySummaryNo { get; set; }

        public object? POSEquipmentNumber { get; set; }

        public object? POSManufacturerSerialNumber { get; set; }

        public object? POSReceiptNo { get; set; }

        public object? POS_CashRegister { get; set; }

        public int PaidToDate { get; set; }

        public int PaidToDateFC { get; set; }

        public int PaidToDateSys { get; set; }

        public string PartialSupply { get; set; }

        public object? PayToBankAccountNo { get; set; }

        public object? PayToBankBranch { get; set; }

        public object? PayToBankCode { get; set; }

        public object? PayToBankCountry { get; set; }

        public string PayToCode { get; set; }

        public string PaymentBlock { get; set; }

        public object? PaymentBlockEntry { get; set; }

        public int PaymentGroupCode { get; set; }

        public string PaymentMethod { get; set; }

        public object? PaymentReference { get; set; }

        public string PeriodIndicator { get; set; }

        public string Pick { get; set; }

        public string PickRemark { get; set; }

        public string PickStatus { get; set; }

        public string PlasticPackagingTaxRelevant { get; set; }

        public object? PointOfIssueCode { get; set; }

        public object? PriceMode { get; set; }

        public string PrintSEPADirect { get; set; }

        public string Printed { get; set; }

        public object? PrivateKeyVersion { get; set; }

        public object? Project { get; set; }

        public object? Receiver { get; set; }

        public object? Reference1 { get; set; }

        public string Reference2 { get; set; }

        public object? RelatedEntry { get; set; }

        public int RelatedType { get; set; }

        public object? Releaser { get; set; }

        public string RelevantToGTS { get; set; }

        public object? ReopenManuallyClosedOrCanceledDocument { get; set; }

        public object? ReopenOriginalDocument { get; set; }

        public object? ReportingSectionControlStatementVAT { get; set; }

        public object? ReqCode { get; set; }

        public int ReqType { get; set; }

        public object? Requester { get; set; }

        public object? RequesterBranch { get; set; }

        public object? RequesterDepartment { get; set; }

        public object? RequesterEmail { get; set; }

        public object? RequesterName { get; set; }

        public object? RequriedDate { get; set; }

        public string Reserve { get; set; }

        public string ReserveInvoice { get; set; }

        public string ReuseDocumentNum { get; set; }

        public string ReuseNotaFiscalNum { get; set; }

        public string Revision { get; set; }

        public string RevisionPo { get; set; }

        public string Rounding { get; set; }

        public int RoundingDiffAmount { get; set; }

        public int RoundingDiffAmountFC { get; set; }

        public int RoundingDiffAmountSC { get; set; }

        public object? SAPPassport { get; set; }

        public int SalesPersonCode { get; set; }

        public int Segment { get; set; }

        public object? SendNotification { get; set; }

        public int SequenceCode { get; set; }

        public string SequenceModel { get; set; }

        public int SequenceSerial { get; set; }

        public int Series { get; set; }

        public string SeriesString { get; set; }

        public int ServiceGrossProfitPercent { get; set; }

        public object? ShipFrom { get; set; }

        public object? ShipPlace { get; set; }

        public object? ShipState { get; set; }

        public object? ShipToCode { get; set; }

        public object? ShipToCodeForReturn { get; set; }

        public string ShowSCN { get; set; }

        public object? SignatureDigest { get; set; }

        public object? SignatureInputMessage { get; set; }

        public object? SpecifiedClosingDate { get; set; }

        public object? StartDeliveryDate { get; set; }

        public object? StartDeliveryTime { get; set; }

        public string StartFrom { get; set; }

        public object? SubSeriesString { get; set; }

        public string Submitted { get; set; }

        public string SummeryType { get; set; }

        public object? Supplier { get; set; }

        public string TaxDate { get; set; }

        public object? TaxExemptionLetterNum { get; set; }

        public Dictionary<string, object> TaxExtension { get; set; }

        public object? TaxInvoiceDate { get; set; }

        public object? TaxInvoiceNo { get; set; }

        public string TaxOnInstallments { get; set; }

        public int TotalDiscount { get; set; }

        public int TotalDiscountFC { get; set; }

        public int TotalDiscountSC { get; set; }

        public int TotalEqualizationTax { get; set; }

        public int TotalEqualizationTaxFC { get; set; }

        public int TotalEqualizationTaxSC { get; set; }

        public object? TrackingNumber { get; set; }

        public object? TransNum { get; set; }

        public int TransportationCode { get; set; }

        public string U_ACT_ComexId { get; set; }

        public object? U_ACT_DAbsCost { get; set; }

        public object? U_ACT_DateEtd { get; set; }

        public object? U_ACT_DatePlanEta { get; set; }

        public object? U_ACT_DatePlanEtd { get; set; }

        public object? U_ACT_DebarkHost { get; set; }

        public string U_ACT_DespType { get; set; }

        public object? U_ACT_DocRef { get; set; }

        public object? U_ACT_ILA { get; set; }

        public object? U_ACT_ILA_DespType { get; set; }

        public object? U_ACT_InstId { get; set; }

        public string U_ACT_IntegrationLog { get; set; }

        public object? U_ACT_Invoice { get; set; }

        public object? U_ACT_LdedCost { get; set; }

        public object? U_ACT_NarwalCode { get; set; }

        public string U_ACT_NfeId { get; set; }

        public object? U_ACT_RefDI { get; set; }

        public object? U_ACT_ShippHost { get; set; }

        public object? U_ACT_TaxId { get; set; }

        public object? U_ACT_TranspChann { get; set; }

        public object? U_AHS_Latitude { get; set; }

        public object? U_AHS_Longitude { get; set; }

        public object? U_AHS_SapUserCode { get; set; }

        public object? U_B1SYS_BPRecCPF { get; set; }

        public object? U_B1SYS_BPRecIBGE { get; set; }

        public object? U_B1SYS_BPRecIE { get; set; }

        public object? U_B1SYS_BPSenCPF { get; set; }

        public object? U_B1SYS_BPSenIBGE { get; set; }

        public object? U_B1SYS_BPSenIE { get; set; }

        public object? U_B1SYS_BpRecCNPJ { get; set; }

        public object? U_B1SYS_BpSenCNPJ { get; set; }

        public object? U_B1SYS_CNPJFromC { get; set; }

        public object? U_B1SYS_CargoTranTyp { get; set; }

        public object? U_B1SYS_ConnTypeCode { get; set; }

        public object? U_B1SYS_EnergConsCls { get; set; }

        public object? U_B1SYS_FisDocStatus { get; set; }

        public int U_B1SYS_ImportProc { get; set; }

        public object? U_B1SYS_MeterCode { get; set; }

        public object? U_B1SYS_ParticipantC { get; set; }

        public object? U_B1SYS_RevIndC510 { get; set; }

        public object? U_B1SYS_RevIndD510 { get; set; }

        public object? U_B1SYS_RevIndD600 { get; set; }

        public object? U_B1SYS_StateOfTP { get; set; }

        public object? U_B1SYS_SubscrCode { get; set; }

        public object? U_B1SYS_TaxID12 { get; set; }

        public object? U_B1SYS_TensGrpCode { get; set; }

        public string U_BoeSavedByService { get; set; }

        public string U_BoeServiceStatus { get; set; }

        public object? U_BoeStatus { get; set; }

        public string U_BoletoGeradoServ { get; set; }

        public string U_ChaveAcesso { get; set; }

        public object? U_ChaveAutenticacao { get; set; }

        public object? U_CodIdent { get; set; }

        public string U_DanfePrintedByServ { get; set; }

        public string U_DanfeServiceStatus { get; set; }

        public object? U_FinCfop { get; set; }

        public string U_GL_IntNar { get; set; }

        public string U_GL_LibPedCom { get; set; }

        public object? U_GL_Obscred { get; set; }

        public string U_GenerateBoeByServ { get; set; }

        public object? U_IB_NumeroFaturaDDA { get; set; }

        public object? U_IMPEX_ILA { get; set; }

        public object? U_IV_IB_CentroDeCusto1 { get; set; }

        public object? U_IV_IB_CentroDeCusto2 { get; set; }

        public object? U_IV_IB_CentroDeCusto3 { get; set; }

        public object? U_IV_IB_CentroDeCusto4 { get; set; }

        public object? U_IV_IB_CentroDeCusto5 { get; set; }

        public object? U_IdentificadorFgts { get; set; }

        public object? U_NossoNumero { get; set; }

        public int U_NrImpressaoNfe { get; set; }

        public int U_NrTentaGerarBoleto { get; set; }

        public int U_OBJ_IndPrazo { get; set; }

        public int U_OBJ_IndPreco { get; set; }

        public int U_OBJ_IndQualid { get; set; }

        public object? U_OBJ_Obs { get; set; }

        public object? U_PRIMOR_DocEntry { get; set; }

        public object? U_PRIMOR_ObjType { get; set; }

        public object? U_SituacaoDocumento { get; set; }

        public object? U_TX_AliqIssNtCtrl { get; set; }

        public object? U_TX_BensIntermediario { get; set; }

        public object? U_TX_CancelId { get; set; }

        public object? U_TX_ChaveCTeSubstituido { get; set; }

        public object? U_TX_ClCons { get; set; }

        public object? U_TX_CodArt { get; set; }

        public object? U_TX_CodCei { get; set; }

        public object? U_TX_CodNatRend { get; set; }

        public object? U_TX_CodObra { get; set; }

        public object? U_TX_CodRespICMSST { get; set; }

        public object? U_TX_CodSeNfts { get; set; }

        public object? U_TX_CondPagamento { get; set; }

        public object? U_TX_DataMIT { get; set; }

        public object? U_TX_DctfCardCode { get; set; }

        public object? U_TX_DctfNDcomp { get; set; }

        public object? U_TX_DctfReceita { get; set; }

        public object? U_TX_DctffPed { get; set; }

        public object? U_TX_DebitoSCPINC { get; set; }

        public string U_TX_DescLinhaDeveDeduzirBc { get; set; }

        public object? U_TX_DesctNfFut { get; set; }

        public object? U_TX_DestOp { get; set; }

        public object? U_TX_DocEntryRef { get; set; }

        public object? U_TX_DocTypeRef { get; set; }

        public object? U_TX_DtComp { get; set; }

        public object? U_TX_DtSaid { get; set; }

        public object? U_TX_EndPres { get; set; }

        public object? U_TX_EnvAuto { get; set; }

        public object? U_TX_ExigibIss { get; set; }

        public object? U_TX_HoraSaid { get; set; }

        public object? U_TX_IEEntrega { get; set; }

        public object? U_TX_INTR_SIS_OR { get; set; }

        public object? U_TX_ImpDmed { get; set; }

        public object? U_TX_IndExiNfe { get; set; }

        public object? U_TX_IndIncentivo { get; set; }

        public object? U_TX_IndInter { get; set; }

        public object? U_TX_IndIssTrib { get; set; }

        public object? U_TX_IndNatFrete { get; set; }

        public object? U_TX_IndPres { get; set; }

        public object? U_TX_InfAliqIssDoc { get; set; }

        public object? U_TX_InstPgtoNfse { get; set; }

        public object? U_TX_IsDeCon { get; set; }

        public object? U_TX_Just { get; set; }

        public object? U_TX_ModoPrest { get; set; }

        public object? U_TX_MotivNRet { get; set; }

        public object? U_TX_MunIncidNfse { get; set; }

        public object? U_TX_NDfe { get; set; }

        public object? U_TX_NatOp { get; set; }

        public object? U_TX_NoSU { get; set; }

        public object? U_TX_NrEncapsNfse { get; set; }

        public object? U_TX_NumDocArrecadacao { get; set; }

        public object? U_TX_NumProc { get; set; }

        public object? U_TX_Ordem { get; set; }

        public object? U_TX_Origem { get; set; }

        public object? U_TX_OrigemIbge { get; set; }

        public object? U_TX_PDV_NF_21_22_Gerado { get; set; }

        public object? U_TX_Participacao { get; set; }

        public object? U_TX_ProdGnre { get; set; }

        public object? U_TX_QueryPrint { get; set; }

        public object? U_TX_RF_IAquis { get; set; }

        public object? U_TX_RF_ICom { get; set; }

        public object? U_TX_RF_Insc_Rural { get; set; }

        public object? U_TX_RF_Serv { get; set; }

        public object? U_TX_RF_TObr { get; set; }

        public object? U_TX_RF_TRep { get; set; }

        public object? U_TX_RegEspTrib { get; set; }

        public object? U_TX_RegEspecialTributacao { get; set; }

        public object? U_TX_RespRetNfse { get; set; }

        public object? U_TX_RetOrgaoPublico { get; set; }

        public object? U_TX_ServicoPrestadoViasPublicas { get; set; }

        public object? U_TX_SetEndNfce { get; set; }

        public object? U_TX_SetIdFisNfce { get; set; }

        public object? U_TX_SigiloRef { get; set; }

        public object? U_TX_Situacao_DES_Vitoria { get; set; }

        public object? U_TX_TagCTe { get; set; }

        public object? U_TX_TermTelPrinc { get; set; }

        public object? U_TX_TermTelef { get; set; }

        public object? U_TX_TipoCliente { get; set; }

        public object? U_TX_TipoNeg { get; set; }

        public object? U_TX_TipoUtilizacao { get; set; }

        public object? U_TX_ToReplace { get; set; }

        public object? U_TX_TpDocArrecada { get; set; }

        public object? U_TX_TpPrazoExp { get; set; }

        public object? U_TX_Ttrib { get; set; }

        public object? U_TX_UFSaidaPais { get; set; }

        public object? U_TX_VlrIssNtCtrl { get; set; }

        public object? U_TX_dPreEntrega { get; set; }

        public object? U_TX_finNFCom { get; set; }

        public object? U_TX_finNFe { get; set; }

        public object? U_TX_indPag { get; set; }

        public object? U_TX_indProc { get; set; }

        public object? U_TX_nProc { get; set; }

        public object? U_TX_tpAto { get; set; }

        public object? U_TX_tpFat { get; set; }

        public object? U_TX_tpNFCredito { get; set; }

        public object? U_TX_tpNFDebito { get; set; }

        public object? U_TX_tpOper { get; set; }

        public object? U_TX_xLocDespacho { get; set; }

        public object? U_TX_xLocExporta { get; set; }

        public object? U_WB_RouteNumber { get; set; }

        public string U_XmlServiceStatus { get; set; }

        public object? U_indProc { get; set; }

        public object? U_nProc { get; set; }

        public string UpdateDate { get; set; }

        public string UpdateTime { get; set; }

        public string UseBillToAddrToDetermineTax { get; set; }

        public string UseCorrectionVATGroup { get; set; }

        public string UseShpdGoodsAct { get; set; }

        public int UserSign { get; set; }

        public string VATRegNum { get; set; }

        public object? VatDate { get; set; }

        public int VatPercent { get; set; }

        public double VatSum { get; set; }

        public double VatSumFc { get; set; }

        public double VatSumSys { get; set; }

        public object? VehiclePlate { get; set; }

        public int WTAmount { get; set; }

        public int WTAmountFC { get; set; }

        public int WTAmountSC { get; set; }

        public int WTApplied { get; set; }

        public int WTAppliedFC { get; set; }

        public int WTAppliedSC { get; set; }

        public int WTExemptedAmount { get; set; }

        public int WTExemptedAmountFC { get; set; }

        public int WTExemptedAmountSC { get; set; }

        public int WTNonSubjectAmount { get; set; }

        public int WTNonSubjectAmountFC { get; set; }

        public int WTNonSubjectAmountSC { get; set; }

        public string WareHouseUpdateType { get; set; }

        public List<object> WithholdingTaxDataCollection { get; set; }

        public List<object> WithholdingTaxDataWTXCollection { get; set; }
    }
}
