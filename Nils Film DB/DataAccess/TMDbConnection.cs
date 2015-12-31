﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows;

namespace Nils_Film_DB.DataAccess
{
    class TMDbConnection
    {

        public async Task<string> Search(string api, string title, string year = null)
        {
            var baseAddress = new Uri("http://api.themoviedb.org/3/");
            using (var httpClient = new HttpClient { BaseAddress = baseAddress })
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                string search;
                if (year != null)
                    search = "search/movie?api_key=" + api + "&include_adult=true&language=de&year=" + year + "&query=" + title;
                else search = "search/movie?api_key=" + api + "&include_adult=true&language=de&query=" + title;
                using (var response = await httpClient.GetAsync(search))
                {
                    string responseData = await response.Content.ReadAsStringAsync();                 
                    return responseData;
                }
            }
        }

        public async Task<string> MovieDetail(string id, string api)
        {
            var baseAddress = new Uri("http://api.themoviedb.org/3/");
            using (var httpClient = new HttpClient { BaseAddress = baseAddress })
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                using (var response = await httpClient.GetAsync("movie/" + id + "?api_key=" + api + "&language=de&append_to_response=alternative_titles,credits,translations"))
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    return responseData;
                }
            }
        }
    }
}
