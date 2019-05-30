using LinqToTwitter;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PulseNestCoreMVC.Hubs;
using PulseNestCoreMVC.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PulseNestCoreMVC.Controllers
{
    public class BackgroundListener : BackgroundService
    {
        static string _consumerKey = "nA3F92Y5tvm9GIWVsVNHDUZbX";
        static string _consumerSecret = "PCH3N0WYT4fFnvRRK1egAJBpjGwg1fRPvnIMlSrJ7ye7vvpefU";
        static string _accessToken = "14935738-TcRyn9YuS43yT3eL0tPttK6XOy99wlSDXTEgVCW4L";
        static string _accessTokenSecret = "lT4A8ZbqRssMQ2YkD8Cwwo8g7QIgftsIWBDUFplz1O23g";
        enum Feel { none, felicidad, ascoEIra, miedoOSorpresa, tristeza };
        private readonly IHubContext<updaterHub, IUpdateMap> _updaterHub;
        private readonly ILogger<BackgroundListener> _logger;
        List<City> cities = new List<City>();
        List<Feeling> feelings = new List<Feeling>();

        private int tweetsProcesados = 0;
        private int tweetsPintados = 0;
        private int tweetsSinCoordenadas = 0;
        private int tweetsSinSentimiento = 0;
        private double procentajeExito = 0.0;
        static int tweetsfelicidad = 0;
        static int tweetsascoEIra = 0;
        static int tweetsmiedoOSorpresa = 0;
        static int tweetstristeza = 0;


        List<mapPoint> pool = new List<mapPoint>();
        Random rdm = new Random();

        static string felicidad = "";
        static string ascoEIra = "";
        static string miedoOSorpresa = "";
        static string tristeza = "";
        static string searchString = "";

        static string[] aFelicidad;
        static string[] aAscoEIra;
        static string[] aMiedoOSorpresa;
        static string[] aTristeza;

        public BackgroundListener(ILogger<BackgroundListener> logger, IHubContext<updaterHub, IUpdateMap> updaterHub)
        {
            _logger = logger;
            _updaterHub = updaterHub;
            try
            {
                using (StreamReader r = new StreamReader(new HttpClient().GetStreamAsync("https://raw.githubusercontent.com/lutangar/cities.json/master/cities.json").Result))
                {
                    string json = r.ReadToEnd();
                    cities = JsonConvert.DeserializeObject<List<City>>(json);
                }
                using (StreamReader r = new StreamReader("Models/feelings.json"))
                {
                    string json = r.ReadToEnd();
                    feelings = JsonConvert.DeserializeObject<List<Feeling>>(json);

                    felicidad = feelings.Find(x => x.name.Contains("felicidad")).words.ToString();
                    ascoEIra = feelings.Find(x => x.name.Contains("ascoEIra")).words.ToString();
                    miedoOSorpresa = feelings.Find(x => x.name.Contains("miedoOSorpresa")).words.ToString();
                    tristeza = feelings.Find(x => x.name.Contains("tristeza")).words.ToString();

                    searchString = tristeza + ", " + ascoEIra + ", " + felicidad + ", " +  miedoOSorpresa;

                    aFelicidad = felicidad.Split(new string[] { ", " }, StringSplitOptions.None);
                    aAscoEIra = ascoEIra.Split(new string[] { ", " }, StringSplitOptions.None);
                    aMiedoOSorpresa = miedoOSorpresa.Split(new string[] { ", " }, StringSplitOptions.None);
                    aTristeza = tristeza.Split(new string[] { ", " }, StringSplitOptions.None);
                }
            }catch(Exception e) { _logger.LogError(e.Message); }
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {           
            listen(stoppingToken);
            listen(stoppingToken);
            drawing(stoppingToken);

        }

        private async Task listen(CancellationToken stoppingToken)
        {
            TwitterContext tc = new TwitterContext(new SingleUserAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = _consumerKey,
                    ConsumerSecret = _consumerSecret,
                    AccessToken = _accessToken,
                    AccessTokenSecret = _accessTokenSecret
                }
            });

            try
                {
                    await (from strm in tc.Streaming
                           where strm.Type == StreamingType.Filter &&
                                   strm.Track == searchString
                           select strm)
                    .StartAsync(async strm =>
                    {
                        processTweet(strm.Content);
                        tweetsProcesados++;
                        procentajeExito = (tweetsPintados * 100) / tweetsProcesados;
                    });
                }
               catch (Exception e) {
                _logger.LogError(e.Message);
                listen(stoppingToken);
            }           

        }

        private Feel analizeFeeling(dynamic json)
        {
            try
            {                            
                string tweet ="";

                if (json.SelectToken("truncated") != null && json.SelectToken("truncated").ToObject<bool>())
                    tweet = json.SelectToken("extended_tweet.full_text").ToObject<string>();
                else 
                    tweet = json.SelectToken("text").ToObject<string>();

                tweet = tweet.ToLower().Replace('#', ' ');

                bool felicidad = aFelicidad.Any(palabra => tweet.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0); ;
                if (felicidad){
                    tweetsfelicidad++;
                    return Feel.felicidad;
                }

                bool ascoEIra = aAscoEIra.Any(palabra => tweet.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0);
                if (ascoEIra) {
                    tweetsascoEIra++;
                    return Feel.ascoEIra;
                }

                bool miedoOSorpresa = aMiedoOSorpresa.Any(palabra => tweet.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0);
                if (miedoOSorpresa) {
                    tweetsmiedoOSorpresa++;
                    return Feel.miedoOSorpresa;
                }

                bool tristeza = aTristeza.Any(palabra => tweet.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0);
                if (tristeza) {
                    tweetstristeza++;
                    return Feel.tristeza;
                }

                if (json.SelectToken("retweeted_status") != null )
                    return analizeFeeling(json.SelectToken("retweeted_status"));

                if (json.SelectToken("quoted_status") != null)
                    return analizeFeeling(json.SelectToken("quoted_status"));

                string mensaje = "Tweet NO pintado (Sentimiento): "+tweet;
                _logger.LogInformation(mensaje);
                tweetsSinSentimiento++;       
                
                return 0;
            }catch (Exception e)
            {
                _logger.LogError(e.Message);
                return 0;
            }
        }

        private string[] analizeCoordinates(dynamic json)
        {
            if (json.SelectToken("coordinates") != null)
            {                
                return json.SelectToken("coordinates.coordinates").ToObject<string[]>();
            }

            if (json.SelectToken("place.bounding_box.coordinates") != null)
            {
                return json.SelectToken("place.bounding_box.coordinates")[0][0].ToObject<string[]>();                  
            }

            if(json.SelectToken("user.location") != null)
            {
                string cityname = json.SelectToken("user.location").ToObject<string>().Split(',')[0].Split('-')[0].Split('/')[0];
                City ciudad = cities.Find(x => x.name.Contains(cityname));
                if(ciudad != null)
                {
                    return new string[] { ciudad.lng, ciudad.lat };
                }
            }

            if(json.SelectToken("retweeted_status") != null)
            {
                string[] reply =  analizeCoordinates(json.SelectToken("retweeted_status"));
                if (reply != null)
                    return reply;
            }

            if (json.SelectToken("quoted_status") != null)
            {
                string[] reply = analizeCoordinates(json.SelectToken("quoted_status"));
                if (reply != null)
                    return reply;
            }

            return null;
        }

        private void processTweet(string tweet)
        {
            try
            {
                dynamic json = JsonConvert.DeserializeObject(tweet);
                mapPoint point = new mapPoint();

                point.coordinates = analizeCoordinates(json);

                if (point.coordinates != null) { 
                    Feel feelingType = analizeFeeling(json);
                    if (feelingType != 0)
                    {
                        switch (feelingType)
                        {
                            case Feel.felicidad:
                                point.color = "red";
                                break;
                            case Feel.ascoEIra:
                                point.color = "#4cff00";
                                break;
                            case Feel.miedoOSorpresa:
                                point.color = "yellow";
                                break;
                            default: //tristeza
                                point.color = "#03c7fc";
                                break;
                        }
                        pool.Add(point);
                    }                   
                }
                else
                {
                    tweetsSinCoordenadas++;
                }
                
            }
            catch (Exception e) { _logger.LogError(e.Message); }
        }

        private async Task drawing(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                if (pool.Count > 0)
                {
                    int index = rdm.Next(0, pool.Count - 1);
                    mapPoint mapP = pool[index];
                    pool.RemoveAt(index);
                    await _updaterHub.Clients.All.ReceivePoint(mapP);                    
                    tweetsPintados++;
                    procentajeExito = (tweetsPintados * 100) / tweetsProcesados;
                }
            }
        }
    }
}
