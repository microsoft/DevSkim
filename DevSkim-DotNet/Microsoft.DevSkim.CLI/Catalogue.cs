// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;

namespace Microsoft.DevSkim.CLI
{
    class Catalogue
    {
        public Catalogue(RuleSet rules)
        {
            _rules = rules;
        }

        public void ToCsv(string fileName, string[] columnList)
        {
            if (columnList.Length == 0)
                columnList = new string[] { "id" };

            using (FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs);
                List<string> lineItems = new List<string>();
                foreach (string col in columnList)
                {
                    lineItems.Add(string.Format("\"{0}\"", col));
                }
                sw.WriteLine(string.Join(",", lineItems));

                foreach (Rule rule in _rules.AsEnumerable())
                {
                    lineItems.Clear();
                    foreach (string col in columnList)
                    {
                        lineItems.Add(string.Format("\"{0}\"",GetProperty(rule, col)));
                    }

                    sw.WriteLine(string.Join(",", lineItems));
                }
                sw.Close();
                fs.Close();
             }
        }

        private string GetProperty(Rule rule, string propName)
        {
            string result = "#PROPERTY NOT FOUND";      

            Type t = typeof(Rule);
            foreach(PropertyInfo property in t.GetProperties())
            {
                foreach(Attribute attr in property.GetCustomAttributes(true))
                {
                    JsonPropertyAttribute jsonAttr = (attr as JsonPropertyAttribute);
                    if (jsonAttr != null && jsonAttr.PropertyName == propName)
                    {
                        return GetPropertyValue(property, rule);
                    }
                }
                
                if (property.Name == propName)
                {
                    return GetPropertyValue(property, rule);
                }
            }

            return result;
        }

        private string GetPropertyValue(PropertyInfo property, Rule rule)
        {
            string result = string.Empty;
            switch (property.PropertyType.Name)
            {
                case "String":
                    result = property.GetValue(rule) as string;
                    break;
                case "String[]":
                    string[] list = (property.GetValue(rule) as string[]);
                    result = (list == null) ? string.Empty : string.Join(",", list);
                    break;
                case "SearchPattern[]":
                case "SearchCondition[]":
                case "CodeFix[]":
                    result = "#UNSUPPORTED PROPERTY";
                    break;
                default:
                    result = property.GetValue(rule).ToString();
                    break;
            }

            return result;
        }

        private RuleSet _rules;
    }
}
