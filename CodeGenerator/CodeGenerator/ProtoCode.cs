using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using SilentOrbit.Code;

namespace SilentOrbit.ProtocolBuffers
{
    static class ProtoCode
    {
        /// <summary>
        /// Generate code for reading and writing protocol buffer messages
        /// </summary>
        public static void Save(ProtoCollection file, Options options)
        {
            string csPath = options.OutputPath;
            CodeWriter.DefaultIndentPrefix = options.UseTabs ? "\t" : "    ";

            string ext = Path.GetExtension(csPath);
            string prefix = csPath.Substring(0, csPath.Length - ext.Length);

            string csDir = Path.GetDirectoryName(csPath);
            if (Directory.Exists(csDir) == false)
                Directory.CreateDirectory(csDir);

            //Basic structures
            using (var cw = new CodeWriter(csPath))
            {
                cw.Comment(@"Classes and structures being serialized");
                cw.WriteLine();
                cw.Comment(@"Generated by ProtocolBuffer
- a pure c# code generation implementation of protocol buffers
Report bugs to: https://silentorbit.com/protobuf/");
                cw.WriteLine();
                cw.Comment(@"DO NOT EDIT
This file will be overwritten when CodeGenerator is run.
To make custom modifications, edit the .proto file and add //:external before the message line
then write the code and the changes in a separate file.");

                cw.WriteLine("using System;");
                cw.WriteLine("using System.Collections.Generic;");
                cw.WriteLine("using Carbon.Shared;");
                cw.WriteLine();

                var messageCode = new MessageCode(cw, options);

                string ns = null; //avoid writing namespace between classes if they belong to the same
                foreach (ProtoMessage m in file.Messages.Values)
                {
                    if (ns != m.CsNamespace)
                    {
                        if (ns != null) //First time
                            cw.EndBracket();
                        cw.Bracket("namespace " + m.CsNamespace);
                        ns = m.CsNamespace;
                    }
                    messageCode.GenerateClass(m);
                    cw.WriteLine();
                }

                foreach (ProtoEnum e in file.Enums.Values)
                {
                    if (ns != e.CsNamespace)
                    {
                        if (ns != null) //First time
                            cw.EndBracket();
                        cw.Bracket("namespace " + e.CsNamespace);
                        ns = e.CsNamespace;
                    }
                    messageCode.GenerateEnum(e);
                    cw.WriteLine();
                }

                if (ns != null)
                    cw.EndBracket();
            }

            //.Serializer.cs
            //Code for Reading/Writing 
            using (var cw = new CodeWriter(prefix + ".Serializer" + ext))
            {
                cw.Comment(@"This is the backend code for reading and writing");
                cw.WriteLine();
                cw.Comment(@"Generated by ProtocolBuffer
- a pure c# code generation implementation of protocol buffers
Report bugs to: https://silentorbit.com/protobuf/");
                cw.WriteLine();
                cw.Comment(@"DO NOT EDIT
This file will be overwritten when CodeGenerator is run.");

                cw.WriteLine("using Carbon.Shared;");
                cw.WriteLine("using System;");
                cw.WriteLine("using System.IO;");
                cw.WriteLine("using System.Text;");
                cw.WriteLine("using System.Collections.Generic;");
                cw.WriteLine();

                string ns = null; //avoid writing namespace between classes if they belong to the same
                foreach (ProtoMessage m in file.Messages.Values)
                {
                    if (ns != m.CsNamespace)
                    {
                        if (ns != null) //First time
                            cw.EndBracket();
                        cw.Bracket("namespace " + m.CsNamespace);
                        ns = m.CsNamespace;
                    }
                    var messageSerializer = new MessageSerializer(cw, options);
                    messageSerializer.GenerateClassSerializer(m);
                }
                if(ns != null)
                    cw.EndBracket();
            }

            //Option to no include ProtocolParser.cs in the output directory
            if (options.NoProtocolParser)
            {
                Console.Error.WriteLine("ProtocolParser.cs not (re)generated");
            }
            else
            {
                string libPath = Path.Combine(Path.GetDirectoryName(csPath), "ProtocolParser.cs");
                using (TextWriter codeWriter = new StreamWriter(libPath, false, Encoding.UTF8))
                {
                    codeWriter.NewLine = "\r\n";
                    ReadCode(codeWriter, "ProtocolParser", true);
                    ReadCode(codeWriter, "ProtocolParserExceptions", false);
                    ReadCode(codeWriter, "ProtocolParserFixed", false);
                    ReadCode(codeWriter, "ProtocolParserKey", false);
                    ReadCode(codeWriter, "ProtocolParserMemory", false);
                    if (options.Net2 == false)
                        ReadCode(codeWriter, "ProtocolParserMemory4", false);
                    ReadCode(codeWriter, "ProtocolParserVarInt", false);
                }
            }
        }

        /// <summary>
        /// Read c# code from sourcePath and write it on code without the initial using statements.
        /// </summary>
        private static void ReadCode(TextWriter code, string name, bool includeUsing)
        {
            code.WriteLine("#region " + name);

            using (TextReader tr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name), Encoding.UTF8))
            {
                while (true)
                {
                    string line = tr.ReadLine();
                    if (line == null)
                        break;
                    if (includeUsing == false && line.StartsWith("using"))
                        continue;

                    if (CodeWriter.DefaultIndentPrefix == "\t")
                        line = line.Replace("    ", "\t");
                    code.WriteLine(line);
                }
            }
            code.WriteLine("#endregion");
        }
    }
}

