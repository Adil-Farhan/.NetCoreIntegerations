using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Globalization;
using TrafficLogixIntegration.ViewModels;
using Microsoft.Extensions.Configuration;
//using System.Configuration;

namespace TCTrafficLogixService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public HttpClient client;
        
        public string GET_TICKETS_URL= "https://api.streetsoncloud.com/v1/tickets/company/";
        public string COMPANY_ID = "3147";
        public string X_API_KEY = "4dXTZA901t6Zr7nQq1ytr4858ImEx8cx7vmwCncI";
        public string GET_TICKET_IMAGE = "https://api.streetsoncloud.com/v1/tickets/image/";
        public string TWAPI_AUTH_TOKEN = "32c9c30f-d6b7-4883-a548-b9ed09e1d2d9";
        public string EXPORT_STATUS_URL = "https://api.streetsoncloud.com/v1/tickets/";
        public string TEK_CONTROL_SERVER = "https://staging.tekcontrol-site.com";
        public string SERVER_API = "/api/speedingticket/save";
        public int MINUTES_TO_DELAY = 1;
        private IConfiguration configuration;


        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            this.configuration = config;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            client = new HttpClient();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            client.Dispose();
            _logger.LogInformation("The service has been stopped...");
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    #region Commented Constants
                    //GET_TICKETS_URL = "https://api.streetsoncloud.com/v1/tickets/company/";
                    //COMPANY_ID = "3147";
                    //X_API_KEY = "4dXTZA901t6Zr7nQq1ytr4858ImEx8cx7vmwCncI";
                    //GET_TICKET_IMAGE = "https://api.streetsoncloud.com/v1/tickets/image/";
                    //TWAPI_AUTH_TOKEN = "32c9c30f-d6b7-4883-a548-b9ed09e1d2d9";
                    //EXPORT_STATUS_URL = "https://api.streetsoncloud.com/v1/tickets/";
                    //TEK_CONTROL_SERVER = "https://staging.tekcontrol-site.com";
                    //SERVER_API = "/api/speedingticket/save";
                    //MINUTES_TO_DELAY = 1; 
                    #endregion

                    GET_TICKETS_URL = configuration.GetSection("GET_TICKETS_URL").Value;
                    COMPANY_ID = configuration.GetSection("COMPANY_ID").Value;
                    X_API_KEY = configuration.GetSection("X_API_KEY").Value;
                    GET_TICKET_IMAGE = configuration.GetSection("GET_TICKET_IMAGE").Value;
                    TWAPI_AUTH_TOKEN = configuration.GetSection("TWAPI_AUTH_TOKEN").Value;
                    EXPORT_STATUS_URL = configuration.GetSection("EXPORT_STATUS_URL").Value;
                    TEK_CONTROL_SERVER = configuration.GetSection("TEK_CONTROL_SERVER").Value;
                    SERVER_API = configuration.GetSection("SERVER_API").Value;
                    MINUTES_TO_DELAY = Int32.Parse( configuration.GetSection("MINUTES_TO_DELAY").Value);


                    List<tcSpeedingTicket> tickets = new List<tcSpeedingTicket>();
                    List<string> ticketsInserted = new List<string>();

                    #region Getting Tickets from url
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri(GET_TICKETS_URL + COMPANY_ID);
                    request.Method = HttpMethod.Get;
                    request.Headers.Add("x-api-key", X_API_KEY);
                    HttpResponseMessage response = await client.SendAsync(request);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var statusCode = response.StatusCode;

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("The Tickets Retrieved Successfully. Status code {StatusCode}", response.IsSuccessStatusCode);
                    }
                    else
                    {
                        _logger.LogInformation("The Tickets Not Retrieved Successfully. Status code {StatusCode}", response.IsSuccessStatusCode);
                    } 
                    #endregion


                    //var jsonList = JObject.Parse(responseString);
                    var singleObjectOfArrays = JObject.Parse(responseString); // parse as array  
                    var ListOFObjectsInArray = JArray.Parse(singleObjectOfArrays.GetValue("tickets").ToString()); 



                    foreach (JObject root in ListOFObjectsInArray)
                    {


                        #region Poppulating tcSpeedTicketObject

                        CultureInfo provider = CultureInfo.InvariantCulture;
                        tcSpeedingTicket ticketObject = new tcSpeedingTicket();
                        ticketObject.ticketId = (String)root["id"];
                        ticketObject.locationId = (String)root["location_id"];
                        ticketObject.createdDate = (String)root["time"];

                        var DateConversion = DateTime.Parse(ticketObject.createdDate);
                        ticketObject.createdDate = DateConversion.ToString("MM/dd/yyyy HH:mm:ss");
                      
                        ticketObject.speedLimit = (String)root["current_speed_limit"];
                        ticketObject.violatingSpeed = (String)root["violating_speed"];
                        ticketObject.licensePlateNumber = (String)root["plate_text"];                        
                        HttpRequestMessage imageRequest = new HttpRequestMessage();
                        imageRequest.RequestUri = new Uri(GET_TICKET_IMAGE + ticketObject.ticketId);
                        imageRequest.Method = HttpMethod.Get;
                        imageRequest.Headers.Add("x-api-key", X_API_KEY);

                        response = await client.SendAsync(imageRequest);
                        var imageArray = await response.Content.ReadAsByteArrayAsync();
                        var imageString = Convert.ToBase64String(imageArray);
                        ticketObject.plateImage = imageString;

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Image for Ticket " + ticketObject.ticketId + " Retrieved Successfully. Status code {StatusCode}", response.IsSuccessStatusCode);
                        }
                        else
                        {
                            _logger.LogInformation("Image for Ticket " + ticketObject.ticketId + " Retrieved Successfully. Status code {StatusCode}", response.IsSuccessStatusCode);
                        }


                        #endregion

                        #region commented additional fields
                        //ticketObject.data = (String)root["data"]; 
                        //ticketObject.locked = (String)root["locked"];
                        ////ticketObject.ocr_status = (String)root["ocr_status"]; 
                        //ticketObject.edited = (String)root["edited"];
                        //ticketObject.user_id = (String)root["user_id"];
                        //ticketObject.vision = (String)root["vision"];
                        //ticketObject.karmen = (String)root["karmen"];
                        //ticketObject.carmen_old = (String)root["carmen_old"];
                        //ticketObject.carmen_general_engine = (String)root["carmen_general_engine"]; 
                        //ticketObject.openalpr = (String)root["openalpr"];
                        //ticketObject.google_v_1 = (String)root["google_v_l"]; 
                        //ticketObject.openalpr_response_plate = (String)root["openalpr_response_plate"];
                        //ticketObject.openalpr_response_car = (String)root["openalpr_response_car"];
                        //ticketObject.type = (String)root["type"];
                        //ticketObject.custom_info = (String)root["custom_info"];
                        //ticketObject.speed_unit = (String)root["speed_unit"];

                        #endregion

                        #region Processing Tickets

                        using (var stringwriter = new System.IO.StringWriter())
                        {
                            #region JSON to XML conversion
                            var serializer = new XmlSerializer(typeof(tcSpeedingTicket));
                            serializer.Serialize(stringwriter, ticketObject);
                            var xmlObj = stringwriter.ToString();
                            xmlObj = xmlObj.Replace("utf-16", "utf-8");
                            ticketsInserted.Add(ticketObject.ticketId);

                            #endregion

                            #region Saving Ticket to TEK Control
                            //send data for insertion to server
                            HttpRequestMessage saveTicketRequest = new HttpRequestMessage();
                            saveTicketRequest.RequestUri = new Uri(TEK_CONTROL_SERVER+ SERVER_API);
                            saveTicketRequest.Content = new StringContent(xmlObj, Encoding.UTF8, "application/xml");
                            


                            saveTicketRequest.Method = HttpMethod.Post;
                            saveTicketRequest.Headers.Add("TWApiAuthToken",TWAPI_AUTH_TOKEN );
                            
                            HttpResponseMessage saveTicketResponse = await client.SendAsync(saveTicketRequest);
                            var saveTicketResponseMeesage = await saveTicketResponse.Content.ReadAsStringAsync();
                        
                            if (saveTicketResponse.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("The Ticket " + ticketObject.ticketId + " Saved Successfully. Status code {StatusCode}", saveTicketResponse.IsSuccessStatusCode);
                            }
                            else
                            {
                                _logger.LogInformation("The Ticket " + ticketObject.ticketId + " Not Saved Successfully. Status code {StatusCode}", saveTicketResponse.IsSuccessStatusCode);
                            }
                            #endregion

                            #region Updating Export Status of Ticket
                            if (saveTicketResponse.IsSuccessStatusCode)
                            {
                                //Update Export Status for this TICKET
                                HttpRequestMessage exportStatusRequest = new HttpRequestMessage();
                                exportStatusRequest.RequestUri = new Uri(EXPORT_STATUS_URL + ticketObject.ticketId + "/export-status/1");
                                exportStatusRequest.Method = HttpMethod.Post;
                                exportStatusRequest.Headers.Add("x-api-key", X_API_KEY);
                                HttpResponseMessage exportResponse = await client.SendAsync(exportStatusRequest);
                                var exportResponseMeesage = await exportResponse.Content.ReadAsStringAsync();
                                var exportResponseStatusCode = exportResponse.StatusCode;

                                if (exportResponse.IsSuccessStatusCode)
                                {
                                    _logger.LogInformation("The Export Status Updated for Ticket " + ticketObject.ticketId + ". Status code {StatusCode}", exportResponse.IsSuccessStatusCode);
                                }
                                else
                                {
                                    _logger.LogInformation("The Export Status Not Updated for Ticket " + ticketObject.ticketId + ". Status code {StatusCode}", exportResponse.IsSuccessStatusCode);
                                }
                            } 
                            #endregion

                        }

                        #endregion
                    }


                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("The website is up. Status code {StatusCode}", response.IsSuccessStatusCode);
                    }
                    else
                    {
                        _logger.LogError("The website is down. Status code {StatusCode}", response.IsSuccessStatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Worker Crashed: {time}", DateTimeOffset.Now); 
                }
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(60000 * MINUTES_TO_DELAY, stoppingToken);
            }
        }
    
    
    
    }

  
}
