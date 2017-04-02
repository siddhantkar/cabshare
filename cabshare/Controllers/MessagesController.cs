﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Json.NET;
using Newtonsoft.Json.Linq;

namespace cabshare
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                var p = await GetFBid(activity);
                await JoinCard.show(activity, connector, activity.ChannelData.ToString()+"        "+ p);
                await ReplyCreate(activity, connector);
                

                
                
            }
            else
            {
                HandleSystemMessage(activity);
            }
            
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;

            
                
            
        }

        private static async Task<LUIS> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            LUIS Data = new LUIS();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/0d2edcc2-6e71-42cd-9ea5-26953a8f2300?subscription-key=f96f048c665e40c0b30a50e790a4de2c&verbose=true&q=" + Query;
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    Data = JsonConvert.DeserializeObject<LUIS>(JsonDataResponse);
                }

            }
            return Data;
        }
        private static async Task<int> ReplyCreate(Activity activity,ConnectorClient connector)
        {
            var x = await GetEntityFromLUIS(activity.Text);
            if (x.topScoringIntent.intent == "Greeting")
            {
                string y = await GetUserName(activity);
                await JoinCard.show(activity,connector,"Hey " + y + "!");
                return 1;

            }
            else if (x.topScoringIntent.intent == "Search")
            {
                
                var a = await GetEntityFromLUIS(activity.Text);
                var y = await DBquery.Clean(a);
                var z = await DBquery.dataquery(y);
                await JoinCard.Cards(activity, connector, z);
                return 1;
                

            }
            else if (x.topScoringIntent.intent == "Add")
            {
                cleandata cleaned = await DBquery.Clean(x);
                if ((cleaned.date == null) || (cleaned.dest == "") || (cleaned.origin == "") || (cleaned.time == default(DateTime)))
                {
                    return await JoinCard.show(activity,connector, "Provide Complete Travel Information.");
                }
                else
                {
                    // ask for max no. people
                    //Activity reply = activity.CreateReply("Please specify the number of seats");
                    //await connector.Conversations.ReplyToActivityAsync(reply);
                    var a = await GetUserName(activity);
                    var b = await GetFBid(activity);
                    string y = await DBquery.addquery(cleaned, a,activity.From.Id,b);
                    return await JoinCard.show(activity, connector, y);
                }
            }
            else if (x.topScoringIntent.intent == "Show")
            {
                
                string y = await GetUserName(activity);
                var z = await DBquery.showdata(y);
                await JoinCard.Cards(activity, connector,z);
                return 1;
            }
            else if (x.topScoringIntent.intent == "Delete")
            {
                string y = await GetUserName(activity);
                using (travelrecordEntities DB = new travelrecordEntities())
                {
                    var match = (from b in DB.Requests where (b.name == y) select b).ToList();
                    foreach (var b in match)
                    {
                        DB.Requests.Remove(b);
                        await DB.SaveChangesAsync();
                    }

                }
                return 1;
            }
            else
            {
                return 1;
            }
        }
        private static async Task<string> GetUserName(Activity activity)
        {
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://graph.facebook.com/v2.6/<user_id>?access_token=EAAeMnBYhrJ8BAMxK2ahazA04uNVNXMuEFTCF3ZC0p9w9ByEGj512nNCq8QA4nKldaBUdH5fiUKO6nZAIwoAZASEoa5s7MB9lFpJks6r0utrEzAfTV00RRZBIDkY3KxQuvcwej3eHBhCT6OpohXJxy0gZBi8Az8pAcclZBdfgPePgZDZD";
                var obj = JObject.Parse(activity.ChannelData.ToString());
                string userid;
                try
                {
                    userid = (obj["sender"]["id"]).ToString();
                }
                catch
                {
                    userid = "";
                }
                RequestURI = RequestURI.Replace("<user_id>", userid);
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    
                    try
                    {
                        var Data = JObject.Parse(JsonDataResponse);
                        string name = Data["first_name"] + " " + Data["last_name"];
                        return name;
                    }
                    catch
                    {
                        return "";
                    }
                }
                else
                {
                    return "";
                }
            }
        }
        private static async Task<string> GetFBid(Activity activity)
        {
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://graph.facebook.com/v2.6/<user_id>?access_token=EAAeMnBYhrJ8BAMxK2ahazA04uNVNXMuEFTCF3ZC0p9w9ByEGj512nNCq8QA4nKldaBUdH5fiUKO6nZAIwoAZASEoa5s7MB9lFpJks6r0utrEzAfTV00RRZBIDkY3KxQuvcwej3eHBhCT6OpohXJxy0gZBi8Az8pAcclZBdfgPePgZDZD";
                var obj = JObject.Parse(activity.ChannelData.ToString());
                string userid;
                try
                {
                    userid = (obj["sender"]["id"]).ToString();
                }
                catch
                {
                    userid = "";
                }
                RequestURI = RequestURI.Replace("<user_id>", userid);
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();

                    try
                    {
                        var Data = JObject.Parse(JsonDataResponse);
                        string name = Data["profile_pic"] + "";
                        var results = new List<string>();
                        //var subjectString = "My Name is #P_NAME# and \r\n I am #P_AGE# years old";
                        Regex regexObj = new Regex("_.+?_");
                        Match matchResults = regexObj.Match(name);
                        while (matchResults.Success)
                        {
                            results.Add(matchResults.ToString().Replace("_", ""));
                            matchResults = matchResults.NextMatch();
                        }
                        return results[0];
                    }
                    catch
                    {
                        return "";
                    }
                }
                else
                {
                    return "";
                }
            }
        }
        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}
