using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CustomerBrokerService;
using System.Configuration;
using System.Threading;
using System.Collections.Concurrent;
using System.Security.AccessControl;
using System.Xml.Linq;

namespace CustomerBrokerService
{
    public partial class Service1 : ServiceBase
    {
        static ConcurrentQueue<logger> cq = new ConcurrentQueue<logger>();
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Thread loggingThread = null, CallGetCustomerBrokerInstructionsThread = null, addBrokerInstructionsThread = null, getDeclarationBackThread = null;
            loggingThread = new Thread(() =>
            {
                RunLoggerThread();
            });
            loggingThread.Start();
            try
            {
                bool Call_getCustomerBrokerInstructions = bool.Parse(ConfigurationManager.AppSettings.Get("Call_getCustomerBrokerInstructions"));
                if (Call_getCustomerBrokerInstructions)
                {
                    CallGetCustomerBrokerInstructionsThread = new Thread(() => CallGetCustomerBrokerInstructions());
                    CallGetCustomerBrokerInstructionsThread.Start();
                }
                bool Call_addBrokerInstructionEvents = bool.Parse(ConfigurationManager.AppSettings.Get("Call_addBrokerInstructionEvents"));
                if (Call_addBrokerInstructionEvents)
                {

                    addBrokerInstructionsThread = new Thread(() => CallAddBrokerInstructionEvents());
                    addBrokerInstructionsThread.Start();
                }

                bool Call_getDeclarationBack = bool.Parse(ConfigurationManager.AppSettings.Get("Call_getDeclarationBack"));
                if (Call_getDeclarationBack)
                {
                    getDeclarationBackThread = new Thread(() => CallGetDeclarationBack());
                    getDeclarationBackThread.Start();
                }
            }
            catch (Exception ex)
            {
                CallGetCustomerBrokerInstructionsThread.Abort();
                addBrokerInstructionsThread.Abort();
                getDeclarationBackThread.Abort();
                log(ex.Message);
                log(ex.StackTrace);

                loggingThread.Abort();
                OnStart(null);
            }

        }

        private static void RunLoggerThread()
        {
            while (true)
            {
                try
                {
                    logger myLogger = null;
                    if (cq.TryDequeue(out myLogger))
                    {
                        using (System.IO.StreamWriter file =
                                       new System.IO.StreamWriter(myLogger.path + "\\AEBCustomsAPILog" + DateTime.Now.ToString("yyyyMMdd") + ".txt", true))
                        {
                            file.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " AEBCustomsAPILog: " + myLogger.data);
                        }
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    Thread.Sleep(1000);
                    using (System.IO.StreamWriter file =
                                     new System.IO.StreamWriter(ConfigurationManager.AppSettings.Get("logpath") + "\\AEBCustomsAPILog" + DateTime.Now.ToString("yyyyMMdd") + ".txt", true))
                    {
                        file.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " AEBCustomsAPILog:" + ex.Message);
                    }

                }
            }
        }

        private void CallGetDeclarationBack()
        {
            while (true)
            {
                try
                {
                    #region configurations for getDeclarationBack
                    var Username_getDeclarationBack = ConfigurationManager.AppSettings.Get("Username_getDeclarationBack");
                    var clientIdentCode_getDeclarationBack = ConfigurationManager.AppSettings.Get("clientIdentCode_getDeclarationBack");
                    var clientSystemId_getDeclarationBack = ConfigurationManager.AppSettings.Get("clientSystemId_getDeclarationBack");
                    var declarationTypeCode_getDeclarationBack = ConfigurationManager.AppSettings.Get("declarationTypeCode_getDeclarationBack");
                    var scenarios_getDeclarationBack = ConfigurationManager.AppSettings.Get("scenarios_getDeclarationBack").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var url_getDeclarationBack = ConfigurationManager.AppSettings.Get("url_getDeclarationBack");
                    var password_getDeclarationBack = ConfigurationManager.AppSettings.Get("password_getDeclarationBack");

                    var storagePath = ConfigurationManager.AppSettings.Get("StoragePath_getDeclarationBack");
                    #endregion
                    //Write code in next milestones



                    bool makeBackupFiles = bool.Parse(ConfigurationManager.AppSettings.Get("MakeBackupFiles_getDeclarationBack"));
                    var BackupPath = ConfigurationManager.AppSettings.Get("BackupPath_getDeclarationBack");
                    BackupPath = BackupPath + "\\" + DateTime.Now.Year + "\\" + DateTime.Now.Month + "\\" + DateTime.Now.Day;



                    // check if we shoudl call customer broker instructions.

                    bool issynccomplete = false;
                    do
                    {
                        #region webrequest
                        log("[getDeclarationBack] Username:" + Username_getDeclarationBack + ",clientIdentCode:" + clientIdentCode_getDeclarationBack + ",password:" + password_getDeclarationBack + ",clientSystemId:" + clientSystemId_getDeclarationBack + ",_url:" + url_getDeclarationBack);

                        // CreateSoapEnvelope for customer broker api call
                        XmlDocument soapEnvelopeXml = CreateSoapEnvelopeForGetChangedDeclarations(Username_getDeclarationBack, clientIdentCode_getDeclarationBack, clientSystemId_getDeclarationBack, declarationTypeCode_getDeclarationBack, scenarios_getDeclarationBack);
                        //make web reqeust for the api call and set its params
                        HttpWebRequest webRequest = CreateWebRequest(url_getDeclarationBack);
                        InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                        webRequest.KeepAlive = true;

                        // Authentication of password and username using basic http
                        string auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(Username_getDeclarationBack + "@" + clientIdentCode_getDeclarationBack + ":" + password_getDeclarationBack));
                        webRequest.Headers.Add("Authorization", auth);

                        //get response
                        IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                        asyncResult.AsyncWaitHandle.WaitOne(3000);

                        string soapResult;
                        XmlDocument docResult = new XmlDocument();

                        using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                        {
                            using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                            {
                                soapResult = rd.ReadToEnd();
                                log("[getDeclarationBack] result:" + soapResult);
                                //for testing 
                                //soapResult = File.ReadAllText("E:\\WebQueryResultResponse-20201125162407.xml");
                                // load response to xml document
                                docResult.LoadXml(soapResult);
                                var checkError = docResult.GetElementsByTagName("hasErrors");
                                // check if response has errors
                                if (checkError[0].InnerText == "false")
                                {
                                    // decode files and save to disk.

                                    XmlNodeList declarations = docResult.GetElementsByTagName("declarations");
                                    foreach (XmlNode declaration in declarations)
                                    {
                                        XmlDocument docDeclarations = new XmlDocument();
                                        docDeclarations.LoadXml("<xml>" + declaration.InnerXml + "</xml>");
                                        XmlNodeList contents = docDeclarations.GetElementsByTagName("data");
                                        XmlNodeList attachmentCodes = docDeclarations.GetElementsByTagName("attachmentCode");
                                        XmlNodeList localreferences = docDeclarations.GetElementsByTagName("localReference");
                                        int i = 0;
                                        foreach (XmlNode content in contents)
                                        {
                                            byte[] data = Convert.FromBase64String(content.InnerText);
                                            string decodedString = Encoding.UTF8.GetString(data);
                                            string extension = "";
                                            if (attachmentCodes[i].InnerText == "WAYBILL" || attachmentCodes[i].InnerText == "UTB")
                                            {
                                                extension = ".pdf";
                                            }
                                            CreateDirectory(storagePath);
                                            System.IO.FileStream stream =
                                                new FileStream(storagePath + "\\" + localreferences[0].InnerText + "-" + attachmentCodes[i].InnerText + extension, FileMode.Create);
                                            using (System.IO.BinaryWriter binaryWriter =
                                            new System.IO.BinaryWriter(stream))
                                            {
                                                binaryWriter.Write(data, 0, data.Length);
                                            }
                                            stream.Close();

                                            if (makeBackupFiles)
                                            {
                                                CreateDirectory(BackupPath);
                                                System.IO.FileStream streambackup =
                                                new FileStream(BackupPath + "\\" + localreferences[0].InnerText + "-" + attachmentCodes[i].InnerText, FileMode.Create);
                                                using (System.IO.BinaryWriter binaryWriter =
                                                new System.IO.BinaryWriter(streambackup))
                                                {
                                                    binaryWriter.Write(data, 0, data.Length);
                                                }
                                                streambackup.Close();

                                                //System.IO.BinaryWriter writerBackup =
                                                //    new BinaryWriter(stream);
                                                //writer.Write(data, 0, data.Length);
                                                //writer.Close();
                                            }
                                            i++;
                                        }

                                    }

                                    var syncIds = docResult.GetElementsByTagName("syncId");
                                    string syncId = "";
                                    //if there is sync id , then acknowledge response.
                                    if (syncIds.Count > 0)
                                    {
                                        syncId = syncIds[0].InnerText;
                                        log("[getDeclarationBack] syncId:" + syncId + ", Calling Acknowledgment for GetChangedDeclarations.");
                                        soapEnvelopeXml = CreateSoapEnvelopeForGetChangedDeclarationsAcknowledgement(Username_getDeclarationBack, clientIdentCode_getDeclarationBack, clientSystemId_getDeclarationBack, syncId, declarationTypeCode_getDeclarationBack);
                                        webRequest = CreateWebRequest(url_getDeclarationBack);
                                        InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                                        webRequest.KeepAlive = true;
                                        webRequest.Headers.Add("Authorization", auth);
                                        asyncResult = webRequest.BeginGetResponse(null, null);
                                        asyncResult.AsyncWaitHandle.WaitOne(3000);
                                        using (WebResponse webResponse1 = webRequest.EndGetResponse(asyncResult))
                                        {
                                            using (StreamReader rd1 = new StreamReader(webResponse.GetResponseStream()))
                                            {
                                                soapResult = rd1.ReadToEnd();
                                                log("[DeclarationACK] soapResult:" + soapResult);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        issynccomplete = true;
                                    }
                                }
                                else
                                {
                                    log("Exception occured:" + soapResult);
                                }
                            }
                        }
                    } while (issynccomplete == false);
                    #endregion

                }
                catch (Exception ex)
                {
                    log("Exception occured:" + ex.Message);
                    log(ex.StackTrace);
                }
                int minutes = 1000 * 60 * int.Parse(ConfigurationManager.AppSettings.Get("IntervalInMinutes_getDeclarationBack"));
                Thread.Sleep(minutes);
            }
        }

        private XmlDocument CreateSoapEnvelopeForGetChangedDeclarationsAcknowledgement(string username_getDeclarationBack, string clientIdentCode_getDeclarationBack, string clientSystemId_getDeclarationBack, string syncId, string declarationTypeCode_getDeclarationBack)
        {
            XmlDocument soapEnvelopeDocument = new XmlDocument();
            string scenariosText = "";
            soapEnvelopeDocument.LoadXml(
                "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:urn=\"urn:de.aeb.xnsg.ic.bf\">" +
                    "<soapenv:Header/>" +
                    "<soapenv:Body>" +
                        "<urn:acknowledgeGetChangedDeclarations>" +
                                "<request>" +
                                "<clientSystemId>" + clientSystemId_getDeclarationBack + "</clientSystemId>" +
                                "<clientIdentCode>" + clientIdentCode_getDeclarationBack + "</clientIdentCode>" +
                                "<userName>" + username_getDeclarationBack + "</userName>" +
                                "<resultLanguageIsoCodes>EN</resultLanguageIsoCodes>" +
                                "<declarationTypeCode>" + declarationTypeCode_getDeclarationBack + "</declarationTypeCode>" +
                                "<syncId>" + syncId + "</syncId>" +
                                scenariosText +
                            "</request>" +
                        "</urn:acknowledgeGetChangedDeclarations>" +
                    "</soapenv:Body>" +
                "</soapenv:Envelope>");
            return soapEnvelopeDocument;
        }

        private void CallAddBrokerInstructionEvents()
        {
            while (true)
            {
                try
                {

                    #region configurations for addBrokerInstructionEvents
                    var url_addBrokerInstructionEvents = ConfigurationManager.AppSettings.Get("url_addBrokerInstructionEvents");
                    var password_addBrokerInstructionEvents = ConfigurationManager.AppSettings.Get("password_addBrokerInstructionEvents");
                    var Username_addBrokerInstructionEvents = ConfigurationManager.AppSettings.Get("Username_addBrokerInstructionEvents");
                    var call_addBrokerInstructionEvents = ConfigurationManager.AppSettings.Get("Call_addBrokerInstructionEvents");
                    var clientIdentCode_addBrokerInstructionEvents = ConfigurationManager.AppSettings.Get("clientIdentCode_addBrokerInstructionEvents");
                    var clientSystemId_addBrokerInstructionEvents = ConfigurationManager.AppSettings.Get("clientSystemId_addBrokerInstructionEvents");
                    var EventChain_addBrokerInstructionEvents = ConfigurationManager.AppSettings.Get("EventChain_addBrokerInstructionEvents");

                    #endregion
                    #region webrequest
                    //get data from db;
                    DBManager dBManager = new DBManager();
                    OrderInstructions orderInstructions = dBManager.GetOrderInstructions();
                    if (orderInstructions != null)
                    {
                        log("[AddBrokerInstruction] orderInstructions fetched from DB. actualDate:" + orderInstructions.actualDate + ",createDate:" + orderInstructions.createDate + ",identCode:" + orderInstructions.identCode + ",localReference:" + orderInstructions.localReference + ",timezoneActualDate:" + orderInstructions.timezoneActualDate + ",timezoneCreateDate:" + orderInstructions.timezoneCreateDate + ",additionalInfo:" + orderInstructions.additionalInfo + ",typeReference:" + orderInstructions.typeReference + ",valueReference:" + orderInstructions.valueReference);
                        log("[AddBrokerInstruction] Username:" + Username_addBrokerInstructionEvents + ",clientIdentCode:" + clientIdentCode_addBrokerInstructionEvents + ",password:" + password_addBrokerInstructionEvents + ",clientSystemId:" + clientSystemId_addBrokerInstructionEvents + ",_url:" + url_addBrokerInstructionEvents);


                        XmlDocument soapEnvelopeXml = CreateSoapEnvelopeForAddBrokerInstructions(Username_addBrokerInstructionEvents, clientIdentCode_addBrokerInstructionEvents, clientSystemId_addBrokerInstructionEvents, orderInstructions);
                        //make web reqeust for the api call and set its params
                        HttpWebRequest webRequest = CreateWebRequest(url_addBrokerInstructionEvents);
                        InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                        webRequest.KeepAlive = true;

                        // Authentication of password and username using basic http
                        string auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(Username_addBrokerInstructionEvents + "@" + clientIdentCode_addBrokerInstructionEvents + ":" + password_addBrokerInstructionEvents));
                        webRequest.Headers.Add("Authorization", auth);

                        //get response
                        IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                        asyncResult.AsyncWaitHandle.WaitOne(3000);

                        string soapResult;
                        XmlDocument docResult = new XmlDocument();

                        using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                        {
                            using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                            {
                                soapResult = rd.ReadToEnd();
                                log("[AddBrokerInstruction] result:" + soapResult);
                                // load response to xml document
                                docResult.LoadXml(soapResult);

                                var checkError = docResult.GetElementsByTagName("hasErrors");
                                // check if response has errors
                                if (checkError[0].InnerText == "false")
                                {
                                    string status = "";
                                    log("[AddBrokerInstruction] No error found. Going to update success in db.");
                                    string[] eventChain = EventChain_addBrokerInstructionEvents.Split(',');
                                    foreach (var item in eventChain)
                                    {
                                        var action = item.Split('=');
                                        if (action[0] == orderInstructions.identCode)
                                        {
                                            status = action[1];
                                            break;
                                        }
                                    }
                                    
                                    dBManager.UpdateCommericalTable(orderInstructions.localReference, status);
                                    dBManager.InsertInLogTable(orderInstructions.localReference, status, "status changed from " + orderInstructions.identCode + " to " + status + ".", "AEBCustomsAPI");


                                }
                                else
                                {
                                    log("[AddBrokerInstruction] Error found. Going to update error failed in db.");
                                    dBManager.UpdateCommericalTable(orderInstructions.localReference, "ERR");
                                    dBManager.InsertInLogTable(orderInstructions.localReference, "ERR", "status changed from " + orderInstructions.identCode + " to ERR. AEBCustomsAPI failed, check logfile for response.", "AEBCustomsAPI");

                                }
                            }
                        }
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        log("[AddBrokerInstruction] No record found in DB.");
                        int minutes = 1000 * 60 * int.Parse(ConfigurationManager.AppSettings.Get("IntervalInMinutes_addBrokerInstructionEvents"));
                        Thread.Sleep(minutes);
                    }
                    #endregion

                }
                catch (Exception ex)
                {
                    log("Exception :" + ex.Message);
                    log("Exception :" + ex.StackTrace);
                    int minutes = 1000 * 60 * int.Parse(ConfigurationManager.AppSettings.Get("IntervalInMinutes_addBrokerInstructionEvents"));
                    Thread.Sleep(minutes);
                }

            }
        }

        private XmlDocument CreateSoapEnvelopeForAddBrokerInstructions(string username_addBrokerInstructionEvents, string clientIdentCode_addBrokerInstructionEvents, string clientSystemId_addBrokerInstructionEvents, OrderInstructions orderInstructions)
        {
            string Documents = "";
            if (orderInstructions.identCode == "TAX")
            {
                string TaxDocumentPath = ConfigurationManager.AppSettings.Get("TaxDocPath_addBrokerInstructionEvents");
                string sTaxDoc = File.ReadAllText(TaxDocumentPath);
                string taxDocString = Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(sTaxDoc));
                Documents = "<documents><name>" + Path.GetFileName(TaxDocumentPath) + "</name><attachmentCode>TAX_FILE</attachmentCode><content>" + taxDocString + "</content></documents>";
            }

            XmlDocument soapEnvelopeDocument = new XmlDocument();
            soapEnvelopeDocument.LoadXml(
            "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:urn=\"urn:de.aeb.xnsg.bis.bf\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                    "<urn:addBrokerInstructionEvents>" +
                        "<requestDTO>" +
                            "<clientSystemId>" + clientSystemId_addBrokerInstructionEvents + "</clientSystemId>" +
                            "<clientIdentCode>" + clientIdentCode_addBrokerInstructionEvents + "</clientIdentCode>" +
                            "<userName>" + username_addBrokerInstructionEvents + "</userName>" +
                            "<resultLanguageIsoCodes>EN</resultLanguageIsoCodes>" +
                            "<createDate>" +
                                "<dateInTimezone>" + orderInstructions.createDate.ToString("yyyy-MM-dd hh:mm:ss") + "</dateInTimezone>" +
                                "<timezone>" + orderInstructions.timezoneCreateDate + "</timezone>" +
                            "</createDate>" +
                            "<localReference>" + orderInstructions.localReference + "</localReference>" +
                            "<events>" +
                                "<identCode>" + orderInstructions.identCode + "</identCode>" +
                                "<actualDate>" +
                                    "<dateInTimezone>" + orderInstructions.actualDate.ToString("yyyy-MM-dd hh:mm:ss") + "</dateInTimezone>" +
                                    "<timezone>" + orderInstructions.timezoneActualDate + "</timezone>" +
                                "</actualDate>" +
                                    "<references>" +
                                    "<type>" + orderInstructions.typeReference + "</type>" +
                                    "<value>" + orderInstructions.valueReference + "</value>" +
                                "</references>" +
                                "<additionalInfo>" + orderInstructions.additionalInfo + "</additionalInfo>" +
                                Documents +
                            "</events>" +
                        "</requestDTO>" +
                    "</urn:addBrokerInstructionEvents>" +
                "</soapenv:Body>" +
            "</soapenv:Envelope>");
            log("[AddBrokerInstruction] <requestDTO>" +
                "<clientSystemId>" + clientSystemId_addBrokerInstructionEvents + "</clientSystemId>" +
                "<clientIdentCode>" + clientIdentCode_addBrokerInstructionEvents + "</clientIdentCode>" +
                "<userName>" + username_addBrokerInstructionEvents + "</userName>" +
                "<resultLanguageIsoCodes>EN</resultLanguageIsoCodes>" +
                "<createDate>" +
                    "<dateInTimezone>" + orderInstructions.createDate.ToString("yyyy-MM-dd hh:mm:ss") + "</dateInTimezone>" +
                    "<timezone>" + orderInstructions.timezoneCreateDate + "</timezone>" +
                "</createDate>" +
                "<localReference>" + orderInstructions.localReference + "</localReference>" +
                "<events>" +
                    "<identCode>" + orderInstructions.identCode + "</identCode>" +
                    "<actualDate>" +
                        "<dateInTimezone>" + orderInstructions.actualDate.ToString("yyyy-MM-dd hh:mm:ss") + "</dateInTimezone>" +
                        "<timezone>" + orderInstructions.timezoneActualDate + "</timezone>" +
                    "</actualDate>" +
                        "<references>" +
                        "<type>" + orderInstructions.typeReference + "</type>" +
                        "<value>" + orderInstructions.valueReference + "</value>" +
                    "</references>" +
                    "<additionalInfo>" + orderInstructions.additionalInfo + "</additionalInfo>" +
                    Documents +
                "</events>" +
            "</requestDTO>");
            return soapEnvelopeDocument;
        }

        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static void CallGetCustomerBrokerInstructions()
        {
            while (true)
            {
                try
                {
                    bool makeBackupFiles = bool.Parse(ConfigurationManager.AppSettings.Get("MakeBackupFiles_getCustomerBrokerInstructions"));
                    var BackupPath = ConfigurationManager.AppSettings.Get("BackupPath_getCustomerBrokerInstructions");
                    BackupPath = BackupPath + "\\" + DateTime.Now.Year + "\\" + DateTime.Now.Month + "\\" + DateTime.Now.Day;
                    #region configurations for customerBrokerInstructions
                    var _url = ConfigurationManager.AppSettings.Get("url_getCustomerBrokerInstructions");
                    var Username = ConfigurationManager.AppSettings.Get("Username_getCustomerBrokerInstructions");
                    var clientIdentCode = ConfigurationManager.AppSettings.Get("clientIdentCode_getCustomerBrokerInstructions");
                    var password = ConfigurationManager.AppSettings.Get("password_getCustomerBrokerInstructions");
                    var clientSystemId = ConfigurationManager.AppSettings.Get("clientSystemId_getCustomerBrokerInstructions");
                    var storagePath = ConfigurationManager.AppSettings.Get("StoragePath_getCustomerBrokerInstructions");
                    var storagePathAttached = ConfigurationManager.AppSettings.Get("StoragePath_Attached_getCustomerBrokerInstructions");

                    #endregion




                    #region webrequest
                    log("[GetBrokerInstructions] Username:" + Username + ",clientIdentCode:" + clientIdentCode + ",password:" + password + ",clientSystemId:" + clientSystemId + ",_url:" + _url);

                    // CreateSoapEnvelope for customer broker api call
                    XmlDocument soapEnvelopeXml = CreateSoapEnvelope(Username, clientIdentCode, clientSystemId);
                    //make web reqeust for the api call and set its params
                    HttpWebRequest webRequest = CreateWebRequest(_url);
                    InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                    webRequest.KeepAlive = true;

                    // Authentication of password and username using basic http
                    string auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(Username + "@" + clientIdentCode + ":" + password));
                    webRequest.Headers.Add("Authorization", auth);

                    //get response
                    IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                    asyncResult.AsyncWaitHandle.WaitOne(3000);

                    string soapResult;
                    XmlDocument docResult = new XmlDocument();

                    using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                    {
                        using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                        {
                            soapResult = rd.ReadToEnd();
                            log("[GetBrokerInstructions] result:" + soapResult);
                            // load response to xml document
                            docResult.LoadXml(soapResult);
                            var checkError = docResult.GetElementsByTagName("hasErrors");
                            // check if response has errors
                            if (checkError[0].InnerText == "false")
                            {
                                // decode files and save to disk.

                                XmlNodeList brokerInstructions = docResult.GetElementsByTagName("brokerInstructions");
                                foreach (XmlNode brokerInstruction in brokerInstructions)
                                {
                                    XmlDocument doc = new XmlDocument();
                                    doc.LoadXml("<xml>" + brokerInstruction.InnerXml + "</xml>");
                                    XmlNodeList contents = doc.GetElementsByTagName("content");
                                    XmlNodeList names = doc.GetElementsByTagName("name");
                                    XmlNodeList localreferences = doc.GetElementsByTagName("localReference");
                                    int i = 0;
                                    foreach (XmlNode content in contents)
                                    {
                                        byte[] data = Convert.FromBase64String(content.InnerText);
                                        string decodedString = Encoding.UTF8.GetString(data);
                                        CreateDirectory(storagePath);
                                        CreateDirectory(storagePathAttached + "\\" + localreferences[0].InnerText);
                                        string pathToSaveFile = "";
                                        if (names[i].InnerText.Contains("xml"))
                                        {
                                            pathToSaveFile = storagePath + "\\" + localreferences[0].InnerText + "-" + names[i].InnerText;
                                        }
                                        else
                                        {
                                            pathToSaveFile = storagePathAttached + "\\" + localreferences[0].InnerText + "\\" + names[i].InnerText;
                                        }
                                        System.IO.FileStream stream =
                                            new FileStream(pathToSaveFile, FileMode.Create);
                                        using (System.IO.BinaryWriter binaryWriter =
                                        new System.IO.BinaryWriter(stream))
                                        {
                                            binaryWriter.Write(data, 0, data.Length);
                                        }
                                        stream.Close();

                                        if (makeBackupFiles)
                                        {
                                            CreateDirectory(BackupPath);
                                            System.IO.FileStream streambackup =
                                            new FileStream(BackupPath + "\\" + localreferences[0].InnerText + "-" + names[i].InnerText, FileMode.Create);
                                            using (System.IO.BinaryWriter binaryWriter =
                                            new System.IO.BinaryWriter(streambackup))
                                            {
                                                binaryWriter.Write(data, 0, data.Length);
                                            }
                                            streambackup.Close();

                                            //System.IO.BinaryWriter writerBackup =
                                            //    new BinaryWriter(stream);
                                            //writer.Write(data, 0, data.Length);
                                            //writer.Close();
                                        }
                                        i++;
                                    }

                                }

                                var syncIds = docResult.GetElementsByTagName("syncId");
                                string syncId = "";
                                //if there is sync id , then acknowledge response.
                                if (syncIds.Count > 0)
                                {
                                    syncId = syncIds[0].InnerText;
                                    log("[GetBrokerInstructions] syncId:" + syncId + ", Calling Acknowledgment");
                                    soapEnvelopeXml = CreateSoapEnvelopeForAcknowledgement(Username, clientIdentCode, clientSystemId, syncId);
                                    webRequest = CreateWebRequest(_url);
                                    InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                                    webRequest.KeepAlive = true;
                                    webRequest.Headers.Add("Authorization", auth);
                                    asyncResult = webRequest.BeginGetResponse(null, null);
                                    asyncResult.AsyncWaitHandle.WaitOne(3000);
                                    using (WebResponse webResponse1 = webRequest.EndGetResponse(asyncResult))
                                    {
                                        using (StreamReader rd1 = new StreamReader(webResponse.GetResponseStream()))
                                        {
                                            soapResult = rd1.ReadToEnd();
                                            log("[BrokerInstructionsAck] soapResult:" + soapResult);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                log("Exception occured:" + soapResult);
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    log("Exception occured:" + ex.Message);
                    log(ex.StackTrace);
                }
                int minutes = 1000 * 60 * int.Parse(ConfigurationManager.AppSettings.Get("IntervalInMinutes_getCustomerBrokerInstructions"));
                Thread.Sleep(minutes);
            }

        }

        private static void log(string data)
        {
            var logPath = ConfigurationManager.AppSettings.Get("logpath");
            CreateDirectory(logPath);
            cq.Enqueue(new logger(logPath, data));

        }
        private static XmlDocument CreateSoapEnvelopeForGetChangedDeclarations(string userName, string clientIdentCode, string clientSystemId, string declarationTypeCode, string[] scenarios)
        {
            XmlDocument soapEnvelopeDocument = new XmlDocument();
            string scenariosText = "";
            foreach (var scenario in scenarios)
            {
                scenariosText += "<scenarios>" + scenario + "</scenarios>";
            }
            soapEnvelopeDocument.LoadXml(
                "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:urn=\"urn:de.aeb.xnsg.ic.bf\">" +
                    "<soapenv:Header/>" +
                    "<soapenv:Body>" +
                        "<urn:getChangedDeclarations>" +
                                "<request>" +
                                "<clientSystemId>" + clientSystemId + "</clientSystemId>" +
                                "<clientIdentCode>" + clientIdentCode + "</clientIdentCode>" +
                                "<userName>" + userName + "</userName>" +
                                "<resultLanguageIsoCodes>EN</resultLanguageIsoCodes>" +
                                "<declarationTypeCode>" + declarationTypeCode + "</declarationTypeCode>" +
                                scenariosText +
                            "</request>" +
                        "</urn:getChangedDeclarations>" +
                    "</soapenv:Body>" +
                "</soapenv:Envelope>");
            return soapEnvelopeDocument;
        }

        private static XmlDocument CreateSoapEnvelope(string userName, string clientIdentCode, string clientSystemId)
        {
            XmlDocument soapEnvelopeDocument = new XmlDocument();
            soapEnvelopeDocument.LoadXml(
            "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:urn=\"urn:de.aeb.xnsg.bis.bf\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                    "<urn:getCustomerBrokerInstructions>" +
                        "<requestDTO>" +
                            "<clientSystemId>" + clientSystemId + "</clientSystemId>" +
                            "<clientIdentCode>" + clientIdentCode + "</clientIdentCode>" +
                            "<userName>" + userName + "</userName>" +
                            "<resultLanguageIsoCodes>EN</resultLanguageIsoCodes>" +
                        "</requestDTO>" +
                    "</urn:getCustomerBrokerInstructions>" +
                "</soapenv:Body>" +
            "</soapenv:Envelope>");
            return soapEnvelopeDocument;
        }

        private static XmlDocument CreateSoapEnvelopeForAcknowledgement(string userName, string clientIdentCode, string clientSystemId, string syncId)
        {
            XmlDocument soapEnvelopeDocument = new XmlDocument();
            soapEnvelopeDocument.LoadXml(
            "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:urn=\"urn:de.aeb.xnsg.bis.bf\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                    "<urn:acknowledgeGetCustomerBrokerInstructions>" +
                        "<requestDTO>" +
                            "<clientSystemId>" + clientSystemId + "</clientSystemId>" +
                            "<clientIdentCode>" + clientIdentCode + "</clientIdentCode>" +
                            "<userName>" + userName + "</userName>" +
                            "<resultLanguageIsoCodes>EN</resultLanguageIsoCodes>" +
                            "<syncId>" + syncId + "</syncId>" +
                        "</requestDTO>" +
                    "</urn:acknowledgeGetCustomerBrokerInstructions>" +
                "</soapenv:Body>" +
            "</soapenv:Envelope>");
            return soapEnvelopeDocument;
        }


        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }
        private static HttpWebRequest CreateWebRequest(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.ContentType = "text/xml;charset=UTF-8";
            webRequest.Accept = "*/*";
            webRequest.Method = "POST";
            return webRequest;
        }


        protected override void OnStop()
        {
        }

        internal void Start()
        {
            this.OnStart(null);
        }
    }

}
