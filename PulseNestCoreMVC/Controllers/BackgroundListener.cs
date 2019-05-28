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
        enum Feeling { none, felicidad, ascoEIra, miedoOSorpresa, tristeza };
        private readonly IHubContext<updaterHub, IUpdateMap> _updaterHub;
        private readonly ILogger<BackgroundListener> _logger;
        List<City> cities = new List<City>();
        private int tweetsProcesados = 0;
        private int tweetsPintados = 0;

        List<mapPoint> pool = new List<mapPoint>();
        Random rdm = new Random();

        

        static string felicidad = "love, appreciate, amor, amo, amar, quiero, aprecio, joy, alegra, alegria, alegría, felicidad, feliz, felices, querido, querida, amado, amada ";
        static string ascoEIra = "hate, odio, repulsión, asco, asqueroso, asquerosidad, cabrea, cabrear, jode, joder, mierda, puto, puta, cabrón, cabron, idiota, imbecil, ira, repulsion, disgust, disgusting, wrath, anger, rage, temper";
        static string miedoOSorpresa = "scare, scared, susto, asustado, miedo, terror, horror, sorpresa, surprise, acojonado, acojonada, asustada, cagado, cagada, temblando, pánico, panico";
        static string tristeza = "sadness, sad, triste, tristeza, pena, sorrow, angustia, anguish, distress, aflicción, aflije, aflige, lástima, lastima";

        static string[] aFelicidad = felicidad.Split(',');
        static string[] aAscoEIra = ascoEIra.Split(',');
        static string[] aMiedoOSorpresa = miedoOSorpresa.Split(',');
        static string[] aTristeza = tristeza.Split(',');

        public BackgroundListener(ILogger<BackgroundListener> logger, IHubContext<updaterHub, IUpdateMap> updaterHub)
        {
            _logger = logger;
            _updaterHub = updaterHub;
            using (StreamReader r = new StreamReader(new HttpClient().GetStreamAsync("https://raw.githubusercontent.com/lutangar/cities.json/master/cities.json").Result))
            {
                string json = r.ReadToEnd();
                cities = JsonConvert.DeserializeObject<List<City>>(json);
            }
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //listen( felicidad, Feeling.felicidad, stoppingToken);
            //listen( ascoEIra, Feeling.ascoEIra, stoppingToken);
            //listen( tristeza, Feeling.tristeza, stoppingToken);
            //listen( miedoOSorpresa, Feeling.miedoOSorpresa, stoppingToken);  
            
            listen(stoppingToken);
            drawing(stoppingToken);

            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation($"Worker running at: {DateTime.Now}");

            //    await drawing(stoppingToken);
            //}

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
            //The list of words that we're going to look for needs to be separated by ","
            //int count = 0;
            try
                {
                    await (from strm in tc.Streaming
                           where strm.Type == StreamingType.Filter &&
                                   strm.Track == felicidad + "," + ascoEIra + "," + miedoOSorpresa + "," + tristeza &&
                                   strm.Locations == "-180,-90,180,90"
                           select strm)
                    .StartAsync(async strm =>
                    {
                        processTweet(strm.Content);
                        tweetsProcesados++;
                    });
                }
               catch (Exception e) {
                _logger.LogError(e.Message);
                //tc.Dispose();
                //listen(words, feelingType, stoppingToken);
            }
            
                

        }

        private Feeling analizeFeeling(dynamic json)
        {
            try
            {                            
                string tweet ="";
                if (json.SelectToken("truncated") != null)
                {
                    if (!json.SelectToken("truncated").ToObject<bool>())
                        tweet = json.SelectToken("text").ToObject<string>();
                    else
                        tweet = json.SelectToken("extended_tweet.full_text").ToObject<string>();

                    bool felicidad = aFelicidad.Any(palabra => tweet.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (felicidad)
                        return Feeling.felicidad;

                    bool ascoEIra = aAscoEIra.Any(palabra => tweet.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (ascoEIra)
                        return Feeling.ascoEIra;

                    bool miedoOSorpresa = aMiedoOSorpresa.Any(palabra => tweet.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (miedoOSorpresa)
                        return Feeling.miedoOSorpresa;

                    bool tristeza = aTristeza.Any(palabra => tweet.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (tristeza)
                        return Feeling.tristeza;
                }
                string mensaje = "Tweet NO pintado (Sentimiento): "+tweet;
                _logger.LogInformation(mensaje);
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

            if (json.SelectToken("retweeted_status.coordinates") != null)
            {
                return json.SelectToken("coordinates.coordinates").ToObject<string[]>();
            }

            if(json.SelectToken("user.location") != null)
            {
                string cityname = json.SelectToken("user.location").ToObject<string>().Split(',')[0];
                City ciudad = cities.Find(x => x.name.Contains(cityname));
                if(ciudad != null)
                {
                    return new string[] { ciudad.lng, ciudad.lat };
                }
            }

            return null;
        }

        private void processTweet(string tweet)
        {
            try
            {
                dynamic json = JsonConvert.DeserializeObject(tweet);
                mapPoint point = new mapPoint();
                Feeling feelingType = analizeFeeling(json);

                if(feelingType != 0) { 
                    switch (feelingType)
                    {
                        case Feeling.felicidad:
                            point.color = "red";
                            break;
                        case Feeling.ascoEIra:
                            point.color = "green";
                            break;
                        case Feeling.miedoOSorpresa:
                            point.color = "yellow";
                            break;
                        default: //tristeza
                            point.color = "blue";
                            break;
                    }

                    point.coordinates = analizeCoordinates(json);
                    if (point.coordinates != null)
                        pool.Add(point);
                    else {
                        string mensaje = "Tweet NO pintado (sin coordenadas)";
                        _logger.LogInformation(mensaje);
                    }
                }
            }
            catch (Exception e) {
                _logger.LogError(e.Message);
            }
        }

        private async Task drawing(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                if (pool.Count > 0)
                {
                    int index = rdm.Next(0, pool.Count - 1);
                    mapPoint mapP = pool[index];
                    await _updaterHub.Clients.All.ReceivePoint(mapP);
                    pool.RemoveAt(index);
                    tweetsPintados++;
                }
            }
        }
    }
}
