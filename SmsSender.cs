using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Entities;
using EskaCMS.Core.Enums;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public class SmsSender : ISmsSender
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;

        private readonly ISiteSettingsService _settingservice;
        public SmsSender(IRepositoryWithTypedId<SiteSettings, string> appSettingRepository, ISiteSettingsService settingservice)
        {
            _appSettingRepository = appSettingRepository;
            _settingservice = settingservice;
        }
        private async Task ZainSendSmsAsync(string Mobile, string Id)
        {
            try
            {
                var SMSProvider = _appSettingRepository.Query().Where(x => x.Id == "SMSConfig").Select(x => x.Value).FirstOrDefault();
                var SMSSetting = JsonConvert.DeserializeObject<SMSconfig>(SMSProvider);
                string content = "New Online Payment Transaction was Registered with Voucher Number : " + Id;
                string DataBody = "{ \"service_type\": \"bulk_sms\", \"recipient_numbers_type\" : \"single_numbers\", \"phone_numbers\" : [\"" + Mobile + "\"], \"content\" : \"" + content + "\", \"sender_id\" : \"" + SMSSetting.sender_id + "\" }";
                byte[] postData = Encoding.ASCII.GetBytes(DataBody);

                // set the request settings
                // set the API URL
                // Define Web Request Variables
                string IntegrationToken = "";
                string IntegrationTokenURL = _appSettingRepository.Query().Where(x => x.Id == "SMSIntegrationTokenURL").Select(x => x.Value).FirstOrDefault();
                string IntegrationTokenUsername = _appSettingRepository.Query().Where(x => x.Id == "SMSIntegrationTokenUsername").Select(x => x.Value).FirstOrDefault();
                string IntegrationTokenPassword = _appSettingRepository.Query().Where(x => x.Id == "SMSIntegrationTokenPassword").Select(x => x.Value).FirstOrDefault();
                string SendSMSUrl = _appSettingRepository.Query().Where(x => x.Id == "SendSMSURL").Select(x => x.Value).FirstOrDefault();
                WebRequest httpRequest;
                WebResponse httpResponse;
                string Result = "";

                // get the Integration Token Key
                httpRequest = WebRequest.Create(IntegrationTokenURL);
                httpRequest.Method = "POST";
                httpRequest.Headers.Add("username", IntegrationTokenUsername);
                httpRequest.Headers.Add("password", IntegrationTokenPassword);
                httpRequest.ContentType = SMSSetting.ContentType;
                // hit the response
                httpResponse = httpRequest.GetResponse();
                using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    while (!reader.EndOfStream)
                        Result = reader.ReadLine();
                }
                // store the Integration Token
                IntegrationToken = JObject.Parse(Result)["result"]["integration_token"].ToString();
                // clear the result
                Result = "";

                // use the Integration Token to send sms
                httpRequest = WebRequest.Create(SendSMSUrl);
                httpRequest.Method = "POST";
                httpRequest.Headers.Add("integration_token", IntegrationToken);
                httpRequest.ContentType = "application/json";
                httpRequest.ContentLength = postData.Length;
                // hit the response
                httpRequest.GetRequestStream().Write(postData, 0, postData.Length);
                // get the response
                httpResponse = httpRequest.GetResponse();
                using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    while (!reader.EndOfStream)
                        Result = reader.ReadLine();
                }
                // write if no api sent
                if (JObject.Parse(Result)["status"].ToString() != "saved successfully!")
                {

                }
                else if (JObject.Parse(Result)["status"].ToString() == "saved successfully!")
                {

                }
            }
            catch (Exception ex)
            {
                string exm = ex.Message;

            }
        }
        public async Task SendSmsAsync(SMSProviders SMSProvider, string PhoneNumber, string Message, long SiteId, string AdditionInfo = "")
        {
            try
            {
                switch (SMSProvider)
                {
                    case SMSProviders.SMSZainJordan:
                        await ZainSendSmsAsync(PhoneNumber, AdditionInfo);
                        break;
                    case SMSProviders.SMSGlobal:
                        await GlobalSMSSendSMSAsync(PhoneNumber, Message, SiteId);
                        break;
                    case SMSProviders.SMSUniFonic:
                        await SendSmsUniFonicAsync(PhoneNumber, Message, SiteId);
                        break;
                    case SMSProviders.SMSVision:
                        await SMSVisionSendSmsAsync(PhoneNumber, Message, SiteId);
                        break;
                    default:
                        break;
                }



            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        private async Task SMSVisionSendSmsAsync(string number, string message, long SiteId)
        {
            try
            {
                var settings = _settingservice.GetSiteSettingById(SMSProviders.SMSVision.ToString(), SiteId).Result;

                var SMSSetting = JsonConvert.DeserializeObject<SMSconfig>(settings.Value);
                //  string content = "New Online Payment Transaction was Registered with Voucher Number : " + "";
                var URL = SMSSetting.URL + "?" + "api_key=" + SMSSetting.APIKey + "&type=text" + "&contacts=" + number + "&senderid=" + SMSSetting.SenderId + "&msg=" + message;
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(URL);
                string resultww = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    var res = Enum.ToObject(typeof(GeneralEnums.SmartVisionReposneCodes), Convert.ToInt32(result));

                }

            }
            catch (Exception e)
            {
                throw e;
            }


        }
        private async Task SendSmsUniFonicAsync(string number, string message, long SiteId) //Der3 (ASIC)
        {
            try
            {
                var settings = _settingservice.GetSiteSettingById(SMSProviders.SMSUniFonic.ToString(), SiteId).Result;
                if (settings == null)
                    throw new Exception("No settings for such provider");

                var SMSSetting = JsonConvert.DeserializeObject<UniFonicSMSVM>(settings.Value);
                var URL = SMSSetting.ApiURL + "?" + "to=" + number + "&msg=" + message + "&sender=" + SMSSetting.Sender + "&appsid=" + SMSSetting.AppsId + "&encoding=" + SMSSetting.Encoding;
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(URL);
                string resultww = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                }

            }
            catch (Exception e)
            {
                throw e;
            }


        }
        private async Task GlobalSMSSendSMSAsync(string Destination, string Message, long SiteId)
        {
            try
            {
                var settings = _settingservice.GetSiteSettingById(SMSProviders.SMSGlobal.ToString(), SiteId).Result;
                if (settings == null)
                    throw new Exception("No settings for such provider");

                var ObjSettings = JsonConvert.DeserializeObject<SMSGlobalSettings>(settings.Value);

                string ApiKey = ObjSettings.ApiKey;// "8c0e4c3e21a6607ed328ab2f1a253017";
                string ApiSecret = ObjSettings.ApiSecret;// "92575d6ce94643eb6356a2bdb17b4699";

                string method = ObjSettings.method;// "GET";
                string host = ObjSettings.host;// "api.smsglobal.com";
                string port = ObjSettings.port;// "443";
                string version = ObjSettings.version;// "v2";
                string path = ObjSettings.path;// "sms";
                //string smsid = ObjSettings.smsid;// "";
                string extradata = string.Empty;
                Uri uri = new Uri(string.Format("https://{0}/{1}/{2}", host, version, path));

                string timestamp = ((int)(DateTime.UtcNow.AddSeconds(-60).Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
                string nonce = Guid.NewGuid().ToString("N");
                string mac = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n", timestamp, nonce, method, uri.PathAndQuery, uri.Host, port, extradata);
                mac = Convert.ToBase64String((new HMACSHA256(Encoding.ASCII.GetBytes(ApiSecret))).ComputeHash(Encoding.ASCII.GetBytes(mac)));

                string authToken = string.Format("id=\"{0}\", ts=\"{1}\", nonce=\"{2}\", mac=\"{3}\"", ApiKey, timestamp, nonce, mac);

                SMSGlobalVM objtoken = new SMSGlobalVM();
                objtoken.destination = Destination;// "971509190910";
                objtoken.message = Message;//.Add(  "TestHomeCuts");
                objtoken.origin = ObjSettings.origin;// "Test";
                string result = string.Empty;
                JObject jsonResult;
                string Json = JsonConvert.SerializeObject(objtoken);
                HttpContent Obj = new StringContent(Json, Encoding.UTF8, "application/json");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("MAC", authToken);
                    //  client.DefaultRequestHeaders.Add("Authorization",string.Format("{0}", authToken));
                    var Base = new Uri(string.Format("{0}://{1}/{2}/{3}", uri.Scheme, uri.Host, version, path));
                    HttpResponseMessage response = client.PostAsync(Base, Obj).Result;
                    result = response.Content.ReadAsStringAsync().Result;
                    if (!response.IsSuccessStatusCode)
                    {
                    }
                }
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }





    }
}
