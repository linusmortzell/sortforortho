using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Models
{
    class OrthoPhotoCreator
    {
        public static void CreateOrthoPhotos(List<List<String>> batchList, string odmUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var content = Task.Run(async () => await InitNewTask(client, odmUrl));
                    Console.WriteLine(content);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


            //if (batchList.Count > 0)
            //{
            //    foreach (List<String> batch in batchList)
            //    {
            //        MultipartFormDataContent content = new MultipartFormDataContent();
            //        for (int i = 0; i < batch.Count; i++)
            //        {
            //            FileStream fs = new FileStream(batch[i], FileMode.Open);
            //            content.Add(new StreamContent(fs), name: "file", fileName: "image" + i);
            //            fs.Close();
            //        }

            //        var request = new HttpRequestMessage(HttpMethod.Post, @url);
            //        request.Headers.Add("accept", "application/json");

            //        var response = await client.SendAsync(request);
            //        if (response.IsSuccessStatusCode)
            //        {
            //            Console.WriteLine(response.Content);
            //            return await response.Content.ReadAsStringAsync();
            //        }
            //        else throw new HttpRequestException($"The remote server returned unexpcted status code: {response.StatusCode} - {response.ReasonPhrase}.");
            //    }
            //}
        }

        private async static Task<String> InitNewTask (HttpClient client, string odmUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, odmUrl + "/task/new/init");
            // request.Headers.Add("accept", "application/json");
            var response = await client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.Content);
                Console.ReadLine();
                return await response.Content.ReadAsStringAsync();
            }
            else throw new HttpRequestException($"The remote server returned unexpcted status code: {response.StatusCode} - {response.ReasonPhrase}.");
        }
    }
}
