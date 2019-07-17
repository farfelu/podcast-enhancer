using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

namespace PodcastEnhancer.Controllers
{
    [Route("")]
    [ApiController]
    public class MainController : ControllerBase
    {
        [HttpGet("")]
        [HttpGet("index.html")]
        [HttpGet("info.html")]
        public IActionResult Info()
        {
            if (!System.IO.File.Exists("index.html"))
            {
                return NotFound();
            }

            return Content(System.IO.File.ReadAllText("index.html"), "text/html");
        }

        [HttpGet("chinesepod/{user}/lessons/{type}/{key}")]
        public IActionResult ChinesepodGet(string user, string type, string key)
        {
            try
            {
                var doc = new XmlDocument();

                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", $"{Request.Headers["User-Agent"].ToString()} FarfeluPodcastEnhancer/1.0 (+https://podcasts.farfelu.eu/info.html)");

                    var reply = client.DownloadString($"https://chinesepod.com/{user}/lessons/{type}/{key}");

                    doc.LoadXml(reply);
                }

                // in case no publish date exists use this one for running dates
                var dateRunner = new DateTime(2000, 1, 1);

                // remove the dialog only feeds and duplicate the others
                foreach (XmlNode item in doc.SelectNodes("/rss/channel/item"))
                {
                    if (item.SelectSingleNode("title").InnerText.StartsWith("Dialogue MP3 - "))
                    {
                        item.ParentNode.RemoveChild(item);
                    }
                    else
                    {
                        // set a publish date if none exists
                        var pubDateNode = item.SelectSingleNode("pubDate");
                        if (pubDateNode == null)
                        {
                            dateRunner = dateRunner.AddMinutes(1);

                            pubDateNode = doc.CreateElement("pubDate");
                            pubDateNode.InnerText = dateRunner.ToString("r");
                            item.AppendChild(pubDateNode);
                        }
                        else if (!DateTime.TryParse(pubDateNode.InnerText, out var publishDate))
                        {
                            dateRunner = dateRunner.AddMinutes(1);
                            pubDateNode.InnerText = dateRunner.ToString("r");
                        }
                        else
                        {
                            dateRunner = publishDate;
                        }


                        var copy = item.CloneNode(true);
                        copy.SelectSingleNode("title").InnerText += " (second time)";
                        copy.SelectSingleNode("guid").InnerText += "#second-time";

                        dateRunner = dateRunner.AddMinutes(1);
                        copy.SelectSingleNode("pubDate").InnerText = dateRunner.ToString("r");
                        item.ParentNode.InsertAfter(copy, item);
                    }
                }

                return Content(doc.OuterXml, "application/xml");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex);
                return NotFound();
            }
        }

    }
}
