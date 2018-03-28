using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json;

namespace TaxiLibrary.Viber
{
    public class ViberMessage
    {
        private static string _viberAuthToken = "Authentication token from your viber public account goes here";
        private static  string _webhookUrl = "Webhook url goes here"; // see SetWebhook method form more info
        #region Athributes
        public int IdViberMessage { get; set; }
        public int? IdAppUser { get; set; }
        public string Receiver { get; set; }
        public sender Sender { get; set; }
        public string Type { get; set; }
        #endregion
        #region InnerClasses
        public class sender
        {
            public string id;
            public string name;
            public string avatar;
            public string country;
            public string language;
            public int api_version;
        }
        public textMessage TextMessage { get; set; }
        public locationMessage LocationMessage { get; set; }
        public trackingData TrackingData { get; set; }
        public class textMessage
        {
            public string ViberUserId { get; set; }
            public string RecivedText { get; set; }
            public string TextToSend { get; set; }
            public string Type { get; set; }
            public string ResponseTrackingData { get; set; } //controls witch tracking data will be saved
            //constructors
            public textMessage()
            {
                Type = "text";
            }
            public textMessage(string viberUserid, string recivedText, string textToSend)
            {
                ViberUserId = viberUserid;
                RecivedText = recivedText;
                TextToSend = textToSend;
                Type = "text";
            }
            //
            //process client request from user
            public void Request()
            {

            }
            //http POST response to user
            public bool Response()
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://chatapi.viber.com/pa/send_message");
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Headers.Add("X-Viber-Auth-Token" + ":" + _viberAuthToken);
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(new
                    {
                        receiver = ViberUserId,
                        type = Type,
                        text = TextToSend,
                        tracking_data = ResponseTrackingData
                    });
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)request.GetResponse();
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                    return false;
                }
            }
            public bool ResponseWithWelcomeMessage()
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://chatapi.viber.com/pa/send_message");
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Headers.Add("X-Viber-Auth-Token" + ":" + _viberAuthToken);
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {

                    string json = JsonConvert.SerializeObject(new
                    {
                        sender = new { name = "taxi chat bot", avatar = "https://cdn.vectorstock.com/i/1000x1000/36/74/chatbot-support-assistant-icon-flat-vector-15313674.jpg" },
                        receiver = ViberUserId,
                        tracking_data = "",
                        type = Type,
                        text = TextToSend
                    });

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)request.GetResponse();
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                    return false;
                }
            }
            public bool ConfirmationResponse()
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://chatapi.viber.com/pa/send_message");
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Headers.Add("X-Viber-Auth-Token" + ":" + _viberAuthToken);
                var btn1 = new
                {

                    Columns = 3,
                    Rows = "1",
                    BgColor = "#2db9b9",
                    BgMediaType = "gif",
                    BgMedia = "http://www.url.by/test.gif",
                    BgLoop = true,
                    ActionType = "reply",
                    ActionBody = "YES",
                    Image = "www.tut.by/img.jpg",
                    Text = "confirm ride",
                    TextVAlign = "middle",
                    TextHAlign = "center",
                    TextOpacity = "60",
                    TextSize = "regular"
                };
                var btn2 = new
                {
                    Columns = 3,
                    Rows = "1",
                    BgColor = "#2db9b9",
                    BgMediaType = "gif",
                    BgMedia = "http://www.url.by/test.gif",
                    BgLoop = true,
                    ActionType = "reply",
                    ActionBody = "NO",
                    Image = "www.tut.by/img.jpg",
                    Text = "cancel",
                    TextVAlign = "middle",
                    TextHAlign = "center",
                    TextOpacity = "60",
                    TextSize = "regular"
                };
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {                    
                    string json = JsonConvert.SerializeObject(new
                    {
                        receiver = ViberUserId,
                        type = Type,
                        text = TextToSend,
                        tracking_data = ResponseTrackingData,
                        keyboard = new
                        {
                            Type = "keyboard",
                            DefaultHeight = true,
                            BgColor = "#FFFFFF",
                            Buttons = new dynamic [2] { btn1, btn2 }
                        }
                     });
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)request.GetResponse();
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
        public class locationMessage
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
            public string Type { get; set; }
            public string ReciverId { get; set; }
            public string StreetName { get; set; }
            //constructor
            public locationMessage(JToken data, string reciverId)
            {
                Lat = data.SelectToken("lat").Value<double>();
                Lon = data.SelectToken("lon").Value<double>();
                Type = "location";
                ReciverId = reciverId;
            }
            public locationMessage()
            {
                //empty constructor
            }

            // process client location message request and find address by lat and long
            public string ClientRequest()
            {
                // add new message to database
                string street = "";
                if (Lat > 0 && Lon > 0)
                {                                     
                    // street = ( find location using REVERSE GEOCODING (lattitude and longitude) with Google or HERE API  )                 
                }
                return street;
            }
            //send response with location message (not used for now)
            public bool Response()
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://chatapi.viber.com/pa/send_message");
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Headers.Add("X-Viber-Auth-Token" + ":" + _viberAuthToken);
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(new
                    {
                        receiver = ReciverId,
                        type = Type,
                        location = new string[2] { "lat: " + Lat, "lon:" + Lon }
                    });
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)request.GetResponse();
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
        //used to track data about user - bot converastion
        //tracking data informations are stored on viber servers 
        public class trackingData
        {
            public bool? ConfirmationSent { get; set; }
            public bool? OrderConfirmed { get; set; }
            public bool? OrderCanceld { get; set; }
            public locationMessage Location { get; set; }
            public DateTime MessageTime { get; set; }
            //constructor
            public trackingData(bool confirmationSent, bool orderConfirmed, bool orderdCalceld, locationMessage location)
            {
                this.ConfirmationSent = confirmationSent;
                this.OrderConfirmed = orderConfirmed;
                this.OrderCanceld = orderdCalceld;
                this.Location = location;
            }
        }
        #endregion
        #region Methods           
        //send welcome sticker message    
        /// <summary>
        /// Webhook for viber api needs to be set only once, for more information visit  https://developers.viber.com/docs/api/rest-bot-api/
        /// </summary>
        /// <returns>true if webhook is set succsesfuly</returns>
        public static bool SetWebhook()
        {
            bool httpResult = false;
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://chatapi.viber.com/pa/set_webhook");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("X-Viber-Auth-Token" + ":" + _viberAuthToken);

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(new
                {
                    url = _webhookUrl,
                    event_types = new string[6]
                    {
                        "delivered",
                        "seen",
                        "failed",
                        "subscribed",
                        "unsubscribed",
                        "conversation_started"
                    }
                });

                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                if (string.Compare(result.Substring(10, 1), "0") == 0) //if response status is zero
                {
                    httpResult = true;
                }
            }
            return httpResult;
        }       
        #endregion
    } 
}
