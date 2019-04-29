using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToTwitter;
using PulseNestCoreMVC.Hubs;
using PulseNestCoreMVC.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PulseNestCoreMVC.Controllers
{
    public class nestListener
    {
        static string _consumerKey = "nA3F92Y5tvm9GIWVsVNHDUZbX";
        static string _consumerSecret = "PCH3N0WYT4fFnvRRK1egAJBpjGwg1fRPvnIMlSrJ7ye7vvpefU";
        static string _accessToken = "14935738-TcRyn9YuS43yT3eL0tPttK6XOy99wlSDXTEgVCW4L";
        static string _accessTokenSecret = "lT4A8ZbqRssMQ2YkD8Cwwo8g7QIgftsIWBDUFplz1O23g";
        enum Feeling { love };
        private updaterHub uH = new updaterHub();

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

        public async Task startListening()
        {
            string love = "love, amor, amo, amar";
            listen(tc, love, Feeling.love);
            drawing();

        }

        private async Task drawing()
        {
            do
            {
                if (pool.Count > 0)
                {
                   await uH.SendNewPoint(pool[rdm.Next(0, pool.Count - 1)]);
                }
            } while (true);
        }

        private async Task listen(TwitterContext twitterCtx, string words, Feeling feelingType)
        {
            //The list of words that we're going to look for needs to be separated by ","
            do
            {
                if (pool.Count < 100)
                {
                    int count = 0;
                    await (from strm in twitterCtx.Streaming
                           where strm.Type == StreamingType.Filter &&
                                 strm.Track == words
                           select strm)
                    .StartAsync(async strm =>
                    {
                        processTweet(strm.Content, feelingType);
                        //if (count++ >= 100)
                        //    strm.CloseStream();
                    });
                }
            } while (true);
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
}
