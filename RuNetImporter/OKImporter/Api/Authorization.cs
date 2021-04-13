using System;
using System.IO;
using System.Windows.Forms;
using rcsir.net.ok.importer.Storages;

namespace rcsir.net.ok.importer.Api
{
    class Authorization
    {
        private RequestParametersStorage parametersStorage;

        internal Authorization(RequestParametersStorage storage)
        {
            parametersStorage = storage;
        }
        
        internal void DeleteCookies()
        {
            DirectoryInfo folder = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Cookies));
            FileInfo[] files = folder.GetFiles();
            foreach (FileInfo file in files) {
                try {
                    File.Delete(file.FullName);
                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
            }
        }

        internal bool IsCodeValid(string stringUrl)
        {
 // Valid response url, f.e.:    "http://rcsoc.spbu.ru/?code=d750985c65.a10a853f71c03cb8de3d27e664ee73f448bfc4ee9d88185e_b87f4d037111f0f1d2ad3866d4ce5cd5_1387810283"
            bool result;
            if (!stringUrl.StartsWith(parametersStorage.RedirectUrl))
                result = false;
            else {
                int index1 = stringUrl.IndexOf("=");
                int index2 = stringUrl.Length;
                parametersStorage.Code = stringUrl.Substring(index1 + 1, index2 - index1 - 1);
                result = true;
            }
            return result;
        }
    }
}
