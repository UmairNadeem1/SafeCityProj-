using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SafeCityProj
{
    public static class logFile
    {
        public static void LogRequestResponse(string message)//, string Request, string Response)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter("ServerLog.txt", true))
                {
                    sw.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ":\t" + message);
                    //sw.WriteLine("Request       : " + (Request != null ? Request.TrimStart(',').Trim() : Request));
                    //sw.WriteLine("Response      : " + (Response != null ? Response.TrimStart(',').Trim() : Response));
                    sw.WriteLine("==================================================================================================================");
                    //sw.Close();
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
