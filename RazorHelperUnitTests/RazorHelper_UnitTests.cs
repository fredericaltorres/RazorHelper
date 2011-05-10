using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DynamicSugar;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using RazorHelperUnitTests;

namespace RazorHelperUnitTests {

    [TestClass]
    public class RazorHelper_UnitTests {

        [TestMethod]
        public void Run_WithIf() {

            var expected = @"
Test: 1
";

            var T = @"
Test:@if(1==1){
 <text>1</text>
}
else{
 <text>2</text>
}
";
            using (var r = new RazorHelper()) {
                Assert.AreEqual(expected, r.Run("T1", T));
            }
        }

        [TestMethod]
        public void Run_i10() {

            var expected = "[L:10]";
            var T        = @"@{ int i = 10; }[L:@i]";

            using (var r = new RazorHelper())
                Assert.AreEqual(expected, r.Run("T1", T));
        }

        [TestMethod]
        public void Run_WithCSFunction() {

            var T = @"
@functions {
  string GetSessionID()
  {
    return String.Format(""{0}-{1}-{2}"", DateTime.Now.ToString(""yyyyMMdd-hh:mm:ss""), System.Environment.MachineName, System.Environment.UserName);
  }
}
SessionID:@GetSessionID()
";
            using (var r = new RazorHelper()) {

                var t = r.Run("T1", T);
                Assert.IsTrue(Regex.IsMatch(t, @"SessionID:\d\d\d\d\d\d\d\d-\d\d:\d\d:\d\d"));
            }
        }

        [TestMethod]
        public void Run_ForI0To10() {

            var expected = "[L:0][L:1][L:2][L:3][L:4][L:5][L:6][L:7][L:8][L:9]";

            var T = @"
@for (int i = 0; i < 10; i++) {
<text>[L:@i]</text>
}
";
            using (var r = new RazorHelper()) {

                Assert.AreEqual(expected, r.Run("T1", T).Replace(Environment.NewLine, ""));
            }
        }

        [TestMethod]
        public void Run_DeclareOneString() {

            var expected = "[John Does]";
            var T        = @"@{ var name = ""John Does""; }[@name]";

            using (var r = new RazorHelper()) {

                Assert.AreEqual(expected, r.Run("T1", T).Replace(Environment.NewLine, ""));
            }
        }

        [TestMethod]
        public void Run_MultiplcationInTemplate() {

            var expected = "Total:0.69104";
            var T        = @"@{ var total = 12.34; var tax   = 5.6; }Total:@(total*tax/100)";

            using (var r = new RazorHelper()) {
                                
                Assert.AreEqual(expected, r.Run("T1", T));
            }
        }

        /*
         * And now let's use Advanced feature of the RazorHelper class.         
         */

        const string BAG_TEMPLATE = @"
var @bag.Class = {
    ID          : @bag.ID,
    LastName    :'@bag.LastName',
    FirstName   :'@bag.FirstName',
    
    Execute: function(i){
        
    }
}
";

        const string BAG_TEMPLATE_RESULT = @"
var User = {
    ID          : 1234,
    LastName    :'DESCARTES',
    FirstName   :'Rene',
    
    Execute: function(i){
        
    }
}
";

        [TestMethod]
        public void Run_JavaScriptTemplate_WithExpandoObject() {

            dynamic bag   = new ExpandoObject();
            bag.LastName  = "DESCARTES";
            bag.FirstName = "Rene";
            bag.Class     = "User";
            bag.ID        = 1234;

            Run_JavaScriptTemplate(bag);
        }

        [TestMethod]
        public void Run_JavaScriptTemplate_WithDynamO() {

            dynamic bag   = new ExpandoObject();
            bag.LastName  = "DESCARTES";
            bag.FirstName = "Rene";
            bag.Class     = "User";
            bag.ID        = 1234;

            Run_JavaScriptTemplate(bag);
        }

        [TestMethod]
        public void Run_JavaScriptTemplate_WithAnonymousObjectType() {

            var bag = new {
                LastName  = "DESCARTES",
                FirstName = "Rene",
                Class     = "User",
                ID        = 1234,
            };
            Run_JavaScriptTemplate(bag);
        }

        private void Run_JavaScriptTemplate(object bag) {

            using (var r = new RazorHelper()) {
                var t = r.Run("JavascriptTemplate", BAG_TEMPLATE, bag);
                Assert.AreEqual(BAG_TEMPLATE_RESULT, t);
            }
        }
        [TestMethod]
        public void __Run_JavaScriptTemplateFromAResourceFile_WithExpandoObject() {

            var javascriptTemplate = DS.Resources.GetTextResource("JavaScript.cshtml", Assembly.GetExecutingAssembly());

            dynamic bag            = new ExpandoObject();
            bag.LastName           = "DESCARTES";
            bag.FirstName          = "Rene";
            bag.Class              = "User";
            bag.ID                 = 1234;

            using (var r = new RazorHelper()) {
                var t = r.Run("JavascriptTemplate", javascriptTemplate, bag);
                Assert.AreEqual(BAG_TEMPLATE_RESULT, t);
            }
        }

        [TestMethod]
        public void Run_WithInstanceOfUserClass() {

            var descartes = Person.GetPeopleList() [0];
            var r         = new RazorHelper();
            var expected  = "Descartes - Rene - 20";
            var T         = "@bag.LastName - @bag.FirstName - @bag.Age";
            var t         = r.Run("PersonTemplate", T, descartes);
            Assert.AreEqual(expected, t);
        }

        [TestMethod]
        public void Run_WithInstanceOfUserClass_2Templates_twice() {

            var descartes = Person.GetPeopleList() [0];
            var r         = new RazorHelper();

            var expected  = "Descartes - Rene - 20";
            var T         = "@bag.LastName - @bag.FirstName - @bag.Age";
            var t         = r.Run("PersonTemplate", T, descartes);
            Assert.AreEqual(expected, t);

            var T2        = "@bag.Age - @bag.LastName - @bag.FirstName";
            var expected2 = "20 - Descartes - Rene";
            var t2        = r.Run("PersonTemplate2", T2, descartes);
            Assert.AreEqual(expected2, t2);

            t             = r.Run("PersonTemplate", T, descartes);
            Assert.AreEqual(expected, t);

            t2            = r.Run("PersonTemplate2", T2, descartes);
            Assert.AreEqual(expected2, t2);
        }

        [TestMethod]
        public void Run_InstanceWithDoubleQuoteInLastName() {

            var Descartes      = Person.GetPeopleList() [0];
            Descartes.LastName = "Descartes\"Descartes";
            var expected       = "Descartes\"Descartes - Rene - 20";
            var T              = "@bag.LastName - @bag.FirstName - @bag.Age";
            RazorHelper r      = new RazorHelper();
            var t              = r.Run("PersonTemplate", T, Descartes);

            Assert.AreEqual(expected, t);
        }
    }
}
