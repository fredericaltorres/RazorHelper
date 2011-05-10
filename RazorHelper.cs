#region License
/*
Dynamic Sugar # Library - DSSharp
Copyright (c) 2011 Frederic Torres

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Razor;
using System.IO;
using System.ComponentModel;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Dynamic;

// Based on Andrew Nurse samples
// http://blog.andrewnurse.net/ct.ashx?id=0e3822d7-3a3b-45d5-ae6f-de88cffbc214&url=http%3a%2f%2fblog.andrewnurse.net%2fcontent%2fbinary%2fHtmlGen.zip
// http://player.microsoftpdc.com/Session/dcced5bc-87d9-4857-9ce9-1b0b887c24b0
// http://blog.andrewnurse.net
// http://www.asp.net/webmatrix/tutorials/2-introduction-to-asp-net-web-programming-using-the-razor-syntax

namespace DynamicSugar {

    /// <summary>
    /// The class is a re usable Razor Template
    /// </summary>
    public abstract class RazorTemplateBase {
                
        public dynamic bag = new ExpandoObject();

        [Browsable(false)]
        public StringBuilder Buffer { get; set; }

        [Browsable(false)]
        public StringWriter Writer { get; set; }

        public RazorTemplateBase() {

            Buffer = new StringBuilder();
            Writer = new StringWriter(Buffer);
        }
        public abstract void Execute();

        public virtual void Write(object value) {

            WriteLiteral(value);
        }
        public virtual void WriteLiteral(object value) {

            Buffer.Append(value);
        }
    }
    /// <summary>
    /// The Razor helper class does the following:
    /// - Simplify executing a Razor template based on a common template
    /// - Cache compiled templates
    /// - Implement the concept of bag populated from any .NET object via reflection
    /// also support Expando Object and DynamO object, to pass variable to be
    /// rendered in the template.
    /// See unit tests from usage sample
    /// </summary>
    public class RazorHelper : IDisposable {

        private RazorTemplateEngine _engine;

        public Dictionary<string, RazorTemplateBase> Templates = new System.Collections.Generic.Dictionary<string, RazorTemplateBase>();
        public string GeneratedCode;

        public List<string> referencedAssemblies = new List<string>(); 

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="namespacesToUse">List of namespaces to imports</param>
        public RazorHelper( List<string> namespacesToUse       = null, 
                            List<string> assembliesToReference = null) {

            string referenceAssembliesV40Folder = String.Format(@"{0}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0", System.Environment.GetEnvironmentVariable("ProgramFiles"));
            string assemblies                   = "Microsoft.CSharp.dll|System.Core.dll";

            foreach(var a in assemblies.Split('|'))
                referencedAssemblies.Add(String.Format(@"{0}\{1}", referenceAssembliesV40Folder, a));
            
            var dSSharpLibraryAssembly = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\");
            referencedAssemblies.Add(dSSharpLibraryAssembly);

            if(assembliesToReference!=null)
                foreach(var a in assembliesToReference)
                    referencedAssemblies.Add(a);

            RazorEngineHost host  = new RazorEngineHost(new CSharpRazorCodeLanguage());
            host.DefaultBaseClass = typeof(RazorTemplateBase).FullName;
            host.DefaultNamespace = "RazorOutput";
            host.DefaultClassName = "Template";

            host.NamespaceImports.Add("System");
            host.NamespaceImports.Add("System.IO");
            host.NamespaceImports.Add("Microsoft.CSharp");

            if(namespacesToUse!=null)
                foreach (var u in namespacesToUse)
                    host.NamespaceImports.Add(u);

            this._engine = new RazorTemplateEngine(host);
        }
        /// <summary>
        /// Helper that execute a razor template, based on the properties of an instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="razorTemplate"></param>
        /// <returns></returns>
        public static string RazorTemplateForInstance(object instance, string razorTemplate) {

            using (var r = new DynamicSugar.RazorHelper()) {

                return r.Run("T1", razorTemplate, instance);
            }
        }              
        /// <summary>
        /// Execute the template named templateName, based on template template,
        /// and eventually use the properties of the instance instance.
        /// If the template name templateName has already been compiled, the compiled
        /// and cached version is used.
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="template"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public string Run(string templateName, string template, object instance = null) {

            if (!Templates.ContainsKey(templateName))
                this.CompileTemplate(templateName, template);

            return this.Run(templateName, instance);
        }
        /// <summary>
        /// Execute the template templateName. The template must have been previously
        /// compiled
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        public string Run(string templateName, object instance = null)
        {
            if (!Templates.ContainsKey(templateName))
                throw new ApplicationException("Razor template is not defined");

            var template                    = this.Templates [templateName];
            RazorTemplateBase b             = template as RazorTemplateBase;
            ExpandoObject bag               = new ExpandoObject(); // Create a new blank bag
            var dictionaryBag               = bag as IDictionary<string, object>;
            b.bag                           = bag;
            IDictionary<string, object> dic = null;
                        
            if ((instance != null) && (instance is ExpandoObject)) // Expando
                dic = instance as IDictionary<string, object>;

            else if (instance != null)                                  // Reflection
                dic =  DynamicSugar.ReflectionHelper.GetDictionary(instance);

            if(dic!=null)
                foreach (KeyValuePair<string, object> i in dic)
                    dictionaryBag.Add(i.Key, i.Value);
                
            template.Execute();
            var s = template.Buffer.ToString();
            template.Buffer.Clear();
            return s;
        }
        /// <summary>
        /// Compiled the template template and add it to the cache
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="template"></param>
        public void CompileTemplate(string templateName, string template) {
                        
            GeneratorResults razorResult = null;

            using (TextReader rdr = new StringReader(template)) {

                razorResult = _engine.GenerateCode(rdr);
            }
            var codeProvider = new CSharpCodeProvider();

            // Generate the code and put it in the text box:
            using (StringWriter sw = new StringWriter()) {

                codeProvider.GenerateCodeFromCompileUnit(razorResult.GeneratedCode, sw, new CodeGeneratorOptions());
                this.GeneratedCode = sw.GetStringBuilder().ToString();
            }
            var outputAssemblyName     = String.Format(@"{0}\DSSharpLibrary_Razor_Template_{1}.dll", Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString("N"));

            var compilerParameters = new CompilerParameters(referencedAssemblies.ToArray(), outputAssemblyName);

            compilerParameters.GenerateInMemory        = true;
            compilerParameters.GenerateExecutable      = false;
            compilerParameters.IncludeDebugInformation = false;
            compilerParameters.CompilerOptions         = "/target:library /optimize";

            var compilerResult = codeProvider.CompileAssemblyFromDom(compilerParameters, razorResult.GeneratedCode);

            if (compilerResult.Errors.HasErrors) {

                var sourceFileName = String.Format(@"{0}\DSSharpLibrary_Razor_Template_{1}.cs", Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString("N"));
                System.IO.File.WriteAllText(sourceFileName, this.GeneratedCode);

                CompilerError err = compilerResult.Errors.OfType<CompilerError>().Where(ce => !ce.IsWarning).First();
                throw new ApplicationException(String.Format("Error Compiling Template: ({0}, {1}) {2}, C# file saved {3}", err.Line, err.Column, err.ErrorText, sourceFileName));
            }
            else {
                Type typ = compilerResult.CompiledAssembly.GetType("RazorOutput.Template");
                RazorTemplateBase newTemplate = Activator.CreateInstance(typ) as RazorTemplateBase;
                if (newTemplate == null)
                    throw new ApplicationException("Could not construct RazorOutput.Template or it does not inherit from TemplateBase");
                else
                    Templates.Add(templateName, newTemplate);
            }
        }
        #region IDisposable Members
        void System.IDisposable.Dispose() {
            
        }
        #endregion
    }
}
