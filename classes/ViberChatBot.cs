using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Data.SqlClient;

namespace TaxiLibrary.Viber
{
    //Important TODO: there needs to be a track of time on each step of oredering ride, becuse conversation tracking_data between bot - user is stored on Viber server's 
    // so if we lost connection to a user we need to reset tracking_data or modfiy bot response next time user sends message
    public class ViberChatBot
    {
        //get user information
        private ViberMessage ProcessSenderInfo(JToken data)
        {
            //TODO: set method calling only once for one user and not each time when user sends message
            // best to do that in subscribed event or conversation started and then when user sends message, 
            //just check for app user in database if not found then get sender info again
            ViberMessage vbMsg = new ViberMessage();
            vbMsg.Sender = new ViberMessage.sender()
            {
                id = data.SelectToken("id").Value<string>(),
                name = data.SelectToken("name").Value<string>(),
                avatar = data.SelectToken("avatar").Value<string>(),
                language = data.SelectToken("language").Value<string>(),
                country = data.SelectToken("country").Value<string>(),
                api_version = data.SelectToken("api_version").Value<int>(),
            };
            return vbMsg;
        }
        //find app third party type user or add new if user doesn't exist
        private DatabasUserSimulation FindViberUser(ViberMessage msg)
        {
            bool userExists = true;  //check if user exists
            DatabasUserSimulation user = null;
            if (!userExists)  //if user doesn't exists add new
            {
                user = new DatabasUserSimulation
                {
                    phone = "", // phone can be added only if user choose to share contanct info
                    name = msg.Sender.name,
                    ViberId = msg.Sender.id               
                };
               /// user.AddUserToDatabase(); save user to database
            }
            else
            {
              // user = GetUserFromDatabase(msg.Sender.id);  //find user by sender_id
            }
            return user;
        }
        //processing viber client request
        public void ProcessClienRequest(JObject data)
        {
            if (data == null)
            { return; }
   
            string Event = data.SelectToken("event").Value<string>();
            switch (Event)
            {
                case "subscribed":
                    //TODO: add welcome message                              
                    break;
                case "webhook":
                    break;
                case "message": // user sent message
                    var messageData = data.SelectToken("message");
                    string tracking_data = data.SelectToken("message.tracking_data")?.Value<string>();  // if tracking_data is address
                    ViberMessage message = ProcessSenderInfo(data.SelectToken("sender"));
                    DatabasUserSimulation user = FindViberUser(message);
                  
                    message.Type = data.SelectToken("message.type").Value<string>();
                    message.IdAppUser = user.IdAppUser;
                    //chacking tracking data
                    if (!string.IsNullOrEmpty(tracking_data))
                    {
                        message.TrackingData = JsonConvert.DeserializeObject<ViberMessage.trackingData>(tracking_data);
                    }
                    // checking message type            
                    switch (message.Type)
                    {
                        case "text":
                            string userText = messageData.SelectToken("text")?.Value<string>();
                            if (string.IsNullOrEmpty(userText))
                            {
                                return; //empty msg sent
                            }
                            //checking if bot is awaiting for location confirmation                     
                            if (message.TrackingData != null && message.TrackingData.ConfirmationSent == true)
                            {
                                if (message.TrackingData.OrderConfirmed == false)   //waiting confirm message              
                                {
                                    message.TextMessage = new ViberMessage.textMessage(user.ViberId, userText, "");
                                    message.LocationMessage = message.TrackingData.Location;
                                    switch (userText.ToUpper())
                                    {
                                        case "YES":
                                            //user confirms address
                                            message.TextMessage.TextToSend = "your order " +
                                            message.LocationMessage.StreetName + " is confirmed you can cancel ride anytime before driver, just send me word \"$cancel\" and I will cancel your ride";
                                            //message.ProcessTarget(message.LocationMessage.Lat, message.LocationMessage.Lon
                                            //    , message.LocationMessage.StreetName);
                                            message.TrackingData.OrderConfirmed = true;  //tracking order confirmation

                                            // TODO: send feedback about driver time of arrival and keep track of validation within that time
                                            // and reset tracking_data when driver arrives
                                            break;
                                        case "NO":
                                            //user canceld address before confirmation
                                            message.TextMessage.TextToSend = "your order " + message.LocationMessage.StreetName
                                                    + " is canceld";
                                            message.TrackingData = null;   // restrating tracking data                                                                                                                         
                                            break;
                                        default:                                        
                                            if (message.Sender.api_version > 3) 
                                            {
                                                message.TextMessage.ResponseTrackingData = message.TrackingData == null ? null : JsonConvert.SerializeObject(message.TrackingData); 
                                                message.TextMessage.TextToSend = "your message " + $"\"{message.TextMessage.RecivedText}\" is invalid, plese confirm your ride";
                                                message.TextMessage.ConfirmationResponse();
                                                return;
                                            }
                                            else
                                            {
                                                message.TextMessage.TextToSend = "your message " + $"\"{message.TextMessage.RecivedText}\""
                                         + " please confirm your order " + message.LocationMessage.StreetName + " by sending text message with \"YES\" to confirm or \"NO\" to cancel";                  
                                            }
                                        
                                            break;
                                    }
                                    message.TextMessage.ResponseTrackingData = message.TrackingData == null ? null : JsonConvert.SerializeObject(message.TrackingData); //sanding tracking data with response 
                                    if (message.TextMessage.Response()) //sending response back to user
                                    {
                                        //adding user message to database                                    
                                        //TODO: add bot message also
                                    }
                                }
                                else if (userText.ToLower() == "$cancel" && message.TrackingData.OrderConfirmed == true) //user wants to cancel after confirmation
                                {
                                    // ask for confirmation and cancel ride if driver has not arrived
                                    message.LocationMessage = message.TrackingData.Location;                                 
                                    message.TextMessage = new ViberMessage.textMessage(user.ViberId, userText,
                                      "");
                                    Object t = new object(); // target simulation
                                    if (t != null)
                                    {
                                        //cancel target by user;

                                        message.TextMessage.TextToSend = "your order " + message.LocationMessage.StreetName + " is canceld";
                                    }
                                    else
                                    {
                                        //not suposed to happen
                                        message.TextMessage.TextToSend = "your order " + message.LocationMessage.StreetName + " is not found";
                                    }

                                    message.TrackingData = null;
                                    message.TextMessage.ResponseTrackingData = null;
                                    if (message.TextMessage.Response())
                                    {
                                       // save message to database
                                    }
                                }
                                else if (message.TrackingData.OrderConfirmed == true)
                                {
                                    // TODO:  user wants to  order one more ride
                                    switch (userText)
                                    {
                                        case "YES":
                                            if (message.TrackingData.OrderCanceld == false)   // waiting for driver arravial
                                            {

                                            }
                                            else if (message.TrackingData.OrderCanceld == true)  // just canceld ride
                                            {

                                            }
                                            break;
                                        default:

                                            break;
                                    }
                                   
                                }                              
                            }
                            else // waiting for location message
                            {
                                message.TextMessage = new ViberMessage.textMessage(user.ViberId, userText, "your message "
                                + $"\"{userText}\"" + " is not valid, please send location message");
                                message.TrackingData = new ViberMessage.trackingData(false, false, false, null);
                                message.TextMessage.ResponseTrackingData = JsonConvert.SerializeObject(message.TrackingData);
                                if (message.TextMessage.Response())
                                {
                                    //save text message to database                                
                                }
                            }
                            break;
                        case "location":
                                               
                            if (message.TrackingData != null && message.TrackingData.ConfirmationSent == true)   // same user sends another address 
                            {
                                // TODO: 
                                if (message.TrackingData.OrderConfirmed == true && message.TrackingData.OrderCanceld == false) //just confirmed
                                {
                                    message.TextMessage = new ViberMessage.textMessage(user.ViberId, "",
                                     "you just confirmed your order for address " + message.TrackingData.Location.StreetName + " are you sure you wont to order new ride?" +
                                     "please confirm by sending \"Y\""
                                        );            // TODO ..                       
                                }
                                else if (message.TrackingData.OrderConfirmed == true && message.TrackingData.OrderCanceld == true) // order confirmed and canceld
                                {
                                    // add validation
                                }
                                else  //order not confirmed but confirmation is sent
                                {
                                    //validation
                                    message.TextMessage = new ViberMessage.textMessage(user.ViberId, "",
                                        "please confirm your order " + message.TrackingData.Location.StreetName + " by sending \"Y\" to confirm or \"N\" to cancel before ordering new ride"
                                        );                                                                                                                                                 
                                }
                                message.TextMessage.ResponseTrackingData = message.TrackingData == null ? null : JsonConvert.SerializeObject(message.TrackingData); //sanding tracking data with response 
                                message.TextMessage.Response();
                            }
                            else
                            {
                                var location = data.SelectToken("message.location");
                                message.LocationMessage = new ViberMessage.locationMessage(location, user.ViberId);
                                //searching location    
                                string street = message.LocationMessage.ClientRequest();                          
                                message.TextMessage = new ViberMessage.textMessage();
                                message.TextMessage.ViberUserId = user.ViberId;
                                if (string.IsNullOrEmpty(street))
                                {
                                    message.TextMessage.TextToSend = "address not found";
                                    message.TrackingData = null;
                                    message.TextMessage.ResponseTrackingData = null;
                                    message.TextMessage.Response(); 
                                }
                                else
                                {
                                    message.TextMessage.TextToSend = "address " + street + " found, please confirm your order";
                                    message.TrackingData = new ViberMessage.trackingData(true, false, false,
                                        message.LocationMessage);
                                    message.TextMessage.ResponseTrackingData = message.TrackingData == null ? null :
                                    JsonConvert.SerializeObject(message.TrackingData); //setting response tracking data
                                    if (message.Sender.api_version > 3)
                                    {
                                        message.TextMessage.ConfirmationResponse();
                                    }
                                    else
                                    {
                                        message.TextMessage.TextToSend += " by sending  \"yes\" to confirm or \"no\" to cancel";
                                        message.TextMessage.Response();
                                    }
                                }
                            
                            
                              
                                                                                      
                             
                            }
                           
                            break;
                    }
                    break;
                case "conversation_started":
                    //triggerd when conversetion is started trough public account url
                    if (data != null)
                    {
                        ViberMessage welcomeMessage = ProcessSenderInfo(data.SelectToken("user"));
                        DatabasUserSimulation viberUser = FindViberUser(welcomeMessage);
                        welcomeMessage.TextMessage = new ViberMessage.textMessage(viberUser.ViberId, "", "welcome user...");

                        if(welcomeMessage.TextMessage.ResponseWithWelcomeMessage())
                        {

                        }
                            
                    }
                    break;
                case "delivered":        //trigged when message is diliverd from bot to user
                    break;
            }

        }      
    }


    public class DatabasUserSimulation
    {
        public int IdAppUser;
        public string phone;
        public string name;
        public string ViberId;
    }

}

