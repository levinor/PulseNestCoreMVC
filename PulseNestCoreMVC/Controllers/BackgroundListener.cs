using LinqToTwitter;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PulseNestCoreMVC.Hubs;
using PulseNestCoreMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        enum Feeling { love };
        private readonly IHubContext<updaterHub, IUpdateMap> _updaterHub;
        private readonly ILogger<BackgroundListener> _logger;

        List<mapPoint> pool = new List<mapPoint>();
        Random rdm = new Random();

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

        string love = "love, want, appreciate, amor, amo, amar, quiero, aprecio";
        string hate = "hate, odio";
        string joy = "joy, alegra, alegria, alegría";
        string sadness = "sadness, sad, triste, tristeza";
        string fear = "scare, scared, susto, asustado, miedo, terror, horror";

        public BackgroundListener(ILogger<BackgroundListener> logger, IHubContext<updaterHub, IUpdateMap> updaterHub)
        {
            _logger = logger;
            _updaterHub = updaterHub;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Worker running at: {DateTime.Now}");
                listen(tc, love, Feeling.love, stoppingToken);
                //listen(tc, hate, Feeling.love, stoppingToken);
                //listen(tc, joy, Feeling.love, stoppingToken);
                //listen(tc, sadness, Feeling.love, stoppingToken);
                //listen(tc, fear, Feeling.love, stoppingToken);
                await drawing(stoppingToken);
            }

        }

        private async Task listen(TwitterContext twitterCtx, string words, Feeling feelingType, CancellationToken stoppingToken)
        {
            //The list of words that we're going to look for needs to be separated by ","

                if (pool.Count < 5)
                {
                    int count = 0;
                    try
                    {
                        await (from strm in twitterCtx.Streaming
                               where strm.Type == StreamingType.Filter &&
                                     strm.Track == words &&
                                     strm.Locations == "-180,-90,180,90"
                               select strm)
                        .StartAsync(async strm =>
                        {
                            processTweet(strm.Content, feelingType);
                            count++;
                            if (count >= 25)
                                strm.CloseStream();
                        });
                    }
                    catch (Exception e) { Console.WriteLine(e); }
                }

        }

        private void processTweet(string tweet, Feeling feelingType)
        {
            dynamic json = JsonConvert.DeserializeObject(tweet);

            if (json.SelectToken("coordinates") != null)
            {
                mapPoint coords = new mapPoint();
                coords.coordinates = json.SelectToken("coordinates.coordinates").ToObject<string[]>();
                coords.color = "Red";
                pool.Add(coords);
            }
            else
            {
                if (json.SelectToken("place.bounding_box.coordinates") != null)
                {
                    mapPoint coords = new mapPoint();
                    coords.coordinates = json.SelectToken("place.bounding_box.coordinates")[0][0].ToObject<string[]>();
                    coords.color = "Red";
                    pool.Add(coords);
                }
                else
                {
                    if (json.SelectToken("retweeted_status.coordinates") != null)
                    {
                        mapPoint coords = new mapPoint();
                        coords.coordinates = json.SelectToken("coordinates.coordinates").ToObject<string[]>();
                        coords.color = "Red";
                        pool.Add(coords);
                    }
                }
            }
        }

        private async Task drawing(CancellationToken stoppingToken)
        {

                if (pool.Count > 0)
                {
                    int index = rdm.Next(0, pool.Count - 1);
                    mapPoint mapP = pool[index];
                    pool.RemoveAt(index);
                    await _updaterHub.Clients.All.ReceivePoint(mapP);                    
                }

        }

    }
}
