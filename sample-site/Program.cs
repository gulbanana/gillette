using Gillette;
using System;
using System.IO;
using System.Linq;

namespace sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var model = new SiteModel();
            var template = File.ReadAllText("Index.cshtml");

            var errors = Razor.Validate<SiteModel>(template);
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ToString());
                }
            }
            else
            {
                var output = Razor.Generate(template, model);
                File.WriteAllText("Index.html", output);
            }
        }
    }
}
