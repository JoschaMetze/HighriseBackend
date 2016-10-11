using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using HighriseBackend.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HighriseBackend.Controllers
{
    [Route("api/[controller]")]
    public class LatestUpdatesController : Controller
    {
        private readonly IOptions<HighriseOptions> _highriseOptions;

        public LatestUpdatesController(IOptions<HighriseOptions> optionsAccessor)
        {
            _highriseOptions = optionsAccessor;
        }
        // GET: api/latestupdates
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var handler = new HttpClientHandler() { Credentials = new NetworkCredential(_highriseOptions.Value.Credential, "x") };
                using (var client = new HttpClient(handler))
                {

                    client.BaseAddress = new Uri(_highriseOptions.Value.Url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("User-Agent", _highriseOptions.Value.UserAgent);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    List<Party> results = new List<Party>();
                    results.AddRange(await getParty(client, 1));
                    results.AddRange(await getParty(client, 3));
                    results.AddRange(await getParty(client, 6));
                    results.AddRange(await getParty(client, 12));
                    return Ok(results);


                }

            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    return BadRequest(e.InnerException.Message);
                else
                    return BadRequest(e.Message);
            }
        }
        protected async Task<IEnumerable<Party>> getParty(HttpClient client, int häufigkeit, int offset = 0)
        {
            string result = null;
            string mockString = String.Format("./mock/mock_{0}_{1}.xml", häufigkeit, offset);
            if (_highriseOptions.Value.UseMock)
            {
                result = System.IO.File.ReadAllText(mockString);
            }
            else
            {
                HttpResponseMessage response = await client.GetAsync("parties/search.xml?kontakthufigkeit_in_monaten=" + häufigkeit + "&n=" + offset);
                if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsStringAsync();
                    if (_highriseOptions.Value.SetupMock)
                    {
                        var writer = System.IO.File.CreateText(mockString);
                        await writer.WriteAsync(result);
                        await writer.FlushAsync();
                        writer.Dispose();
                    }
                }
            }
            if (result == null)
                return new List<Party>();
            XElement partiesElement = XElement.Parse(result);
            List<Party> resultParties = new List<Party>();
            foreach (var party in partiesElement.Elements("party"))
            {
                string noteURL = "";
                Party newParty = new Party();
                newParty.Type = party.Attribute("type").Value;
                newParty.Haeufigkeit = häufigkeit;
                newParty.Id = party.Element("id").Value;
                newParty.Status = "rot";
                if (newParty.Type == "Company")
                {
                    newParty.Name = party.Element("name").Value;
                    noteURL = String.Format("companies/{0}/notes.xml", newParty.Id);
                }
                else
                {
                    newParty.Name = party.Element("first-name").Value + " " + party.Element("last-name").Value;
                    if (party.Element("company-name") != null)
                        newParty.FirmenName = party.Element("company-name").Value;
                    noteURL = String.Format("people/{0}/notes.xml", newParty.Id);
                }
                newParty.Aktualisiert = DateTime.Parse(party.Element("updated-at").Value);
                if ((DateTime.Now - newParty.Aktualisiert).TotalDays < 30 * häufigkeit)
                    newParty.Status = "gelb";
                var ansprechPartner = party.XPathSelectElements("subject_datas/subject_data[subject_field_id=652041]/value");
                if (ansprechPartner != null && ansprechPartner.Count() > 0)
                {
                    newParty.Ansprechpartner = ansprechPartner.First().Value;
                }
                //get notes
                string notesResult = null;
                string noteMock = String.Format("./mock/{0}.xml", newParty.Id);
                if (!_highriseOptions.Value.UseMock)
                {
                    HttpResponseMessage noteResponse = await client.GetAsync(noteURL);
                    if (noteResponse.IsSuccessStatusCode)
                    {
                        notesResult = await noteResponse.Content.ReadAsStringAsync();
                        if (_highriseOptions.Value.SetupMock)
                        {
                            var writer = System.IO.File.CreateText(noteMock);
                            await writer.WriteAsync(notesResult);
                            await writer.FlushAsync();
                            writer.Dispose();
                        }
                    }
                }
                else
                {
                    notesResult = System.IO.File.ReadAllText(noteMock);

                }
                if (notesResult != null)
                {
                    XElement notes = XElement.Parse(notesResult);
                    var sortedNotes = notes.Elements("note").OrderByDescending(note => DateTime.Parse(note.Element("updated-at").Value).Ticks);
                    if (sortedNotes.Count() > 0)
                    {
                        newParty.LetzterKommentar = sortedNotes.First().Element("body").Value.Replace("\n", "<br/>");
                        newParty.LetzterKommentarAm = DateTime.Parse(sortedNotes.First().Element("updated-at").Value);
                        if ((DateTime.Now - newParty.LetzterKommentarAm).TotalDays < 30 * häufigkeit)
                            newParty.Status = "grün";
                    }
                }
                resultParties.Add(newParty);
            }
            if (partiesElement.Elements("party").Count() == 25)
            {
                //recurse
                resultParties.AddRange(await getParty(client, häufigkeit, offset + 25));
            }

            return resultParties;


        }


    }
}
