using System;
using SilentOrbit.Code;
using System.Collections.Generic;

namespace SilentOrbit.ProtocolBuffers
{
    class MessageCode
    {
        readonly CodeWriter cw;
        readonly Options options;

        public MessageCode(CodeWriter cw, Options options)
        {
            this.cw = cw;
            this.options = options;
        }

        public void GenerateClass(ProtoMessage m)
        {
            if (options.NoGenerateImported && m.IsImported)
            {
                Console.Error.WriteLine("Skipping imported " + m.FullProtoName);
                return;
            }

            //Do not generate class code for external classes
            if (m.OptionExternal)
            {
                cw.Comment("Written elsewhere");
                cw.Comment(m.OptionAccess + " " + m.OptionType + " " + m.CsType + " {}");
                return;
            }

            //Default class
            cw.Summary(m.Comments);
            cw.Bracket(m.OptionAccess + " partial " + m.OptionType + " " + m.CsType + " : IProtocolMessage");

            if (options.GenerateDefaultConstructors)
                GenerateCtorForDefaults(m);
            GenerateFullName(m);
            GenerateEnums(m);

            GenerateProperties(m);

            //if(options.GenerateToString...
            // ...

            if (m.OptionPreserveUnknown)
            {
                cw.Summary("Values for unknown fields.");
                cw.WriteLine("public List<global::SilentOrbit.ProtocolBuffers.KeyValue> PreservedFields;");
                cw.WriteLine();
            }

            if (m.OptionTriggers)
            {
                cw.Comment("protected virtual void BeforeSerialize() {}");
                cw.Comment("protected virtual void AfterDeserialize() {}");
                cw.WriteLine();
            }

            foreach (ProtoMessage sub in m.Messages.Values)
            {
                GenerateClass(sub);
                cw.WriteLine();
            }
            cw.WriteLine("public byte [] ToByteArray() {return "+m.CsType+".SerializeToBytes(this);}");
            cw.EndBracket();
            return;
        }

        private void GenerateFullName(ProtoMessage m)
        {
            cw.WriteLine("public static readonly string PROTO_NAME = \"" + m.FullProtoName + "\";");
            cw.WriteLine("public string ProtoName { get{return PROTO_NAME;}}");
        }


        void GenerateCtorForDefaults(ProtoMessage m)
        {
            // Collect all fields with default values.
            var fieldsWithDefaults = new List<Field>();
            foreach (Field field in m.Fields.Values)
            {
                if (field.OptionDefault != null)
                {
                    fieldsWithDefaults.Add(field);
                }
            }

            if (fieldsWithDefaults.Count > 0)
            {
                cw.Bracket("public " + m.CsType + "()");
                foreach (var field in fieldsWithDefaults)
                {
                    string formattedValue = field.FormatDefaultForTypeAssignment();
                    string line = string.Format("{0} = {1};", field.CsName, formattedValue);
                    cw.WriteLine(line);
                }
                cw.EndBracket();
            }
        }

        void GenerateEnums(ProtoMessage m)
        {
            foreach (ProtoEnum me in m.Enums.Values)
            {
                GenerateEnum(me);
            }
        }

        public void GenerateEnum(ProtoEnum m)
        {
            if (options.NoGenerateImported && m.IsImported)
            {
                Console.Error.WriteLine("Skipping imported enum " + m.FullProtoName);
                return;
            }

            if (m.OptionExternal)
            {
                cw.Comment("Written elsewhere");
                cw.Comment(m.Comments);
                cw.Comment(m.OptionAccess + " enum " + m.CsType);
                cw.Comment("{");
                foreach (var epair in m.Enums)
                {
                    cw.Summary(epair.Comment);
                    cw.Comment(cw.IndentPrefix + epair.Name + " = " + epair.Value + ",");
                }
                cw.Comment("}");
                return;
            }

            cw.Summary(m.Comments);
            if (m.OptionFlags)
                cw.Attribute("global::System.FlagsAttribute");
            cw.Bracket(m.OptionAccess + " enum " + m.CsType);
            foreach (var epair in m.Enums)
            {
                cw.Summary(epair.Comment);
                cw.WriteLine(epair.Name + " = " + epair.Value + ",");
            }
            cw.EndBracket();
            cw.WriteLine();
        }

        /// <summary>
        /// Generates the properties.
        /// </summary>
        /// <param name='template'>
        /// if true it will generate only properties that are not included by default, because of the [generate=false] option.
        /// </param>
        void GenerateProperties(ProtoMessage m)
        {
            WriteState(m);
            WriteReadOnly(m);
            WriteMarkDirty(m);
            WriteIsDirty(m);
            WriteClearDirty(m);
            WriteFields(m);
            WriteEquals(m);
            WriteGetHashCode(m);
        }

        private void WriteState(ProtoMessage m)
        {
            cw.WriteLine(@"
private readonly State _private = new State();
private class State {
    internal bool ro;
    internal bool dirty;
            ");

            foreach (Field f in m.Fields.Values)
            {
                cw.WriteLine("    " + GenerateProperty(f, false));
            }

            cw.WriteLine("}");
        }

        private void WriteReadOnly(ProtoMessage m)
        {
            cw.WriteLine(@"
private bool ro;
public void MakeReadOnly() {
    _private.ro = true;
");

            foreach (Field f in m.Fields.Values)
            {
                if (f.Rule == FieldRule.Repeated || f.ProtoType is ProtoMessage)
                {
                    cw.WriteLine("    _private._"+f.ProtoName+".MakeReadOnly();");
                }
            }

            cw.WriteLine("}");
        }

        private void WriteMarkDirty(ProtoMessage m)
        {
            cw.WriteLine(@"
protected void MarkDirty() {
    Check.State(!ro, ""Cannot modify read only entity;"");
    _private.dirty = true;
}");

        }

        private void WriteIsDirty(ProtoMessage m)
        {
            cw.WriteLine(@"
public bool IsDirty
{
    get
    {
        if (_private.dirty) return true;"
            );

            foreach (Field f in m.Fields.Values)
            {
                if (!(f.ProtoType is ProtoMessage))
                {
                    continue;
                }

                cw.WriteLine(@"
        if (_private._" + f.ProtoName + ".IsDirty) return true;");
            }

            cw.WriteLine(@"
        return false;
    }
}"
                );
        }

        private void WriteClearDirty(ProtoMessage m)
        {
            cw.WriteLine(@"
public void ClearDirty()
{
    _private.dirty = false;"
            );

            foreach (Field f in m.Fields.Values)
            {
                if (!(f.ProtoType is ProtoMessage))
                {
                    continue;
                }
                cw.WriteLine("    _private._" + f.ProtoName + ".ClearDirty();");
            }
            cw.WriteLine(@"

}"
                );
        }

        private void WriteFields(ProtoMessage m)
        {
            foreach (Field f in m.Fields.Values)
            {
                if (f.Comments != null)
                    cw.Summary(f.Comments);

                if (f.OptionExternal)
                {
                    if (f.OptionDeprecated)
                        cw.WriteLine("// [Obsolete]");
                    cw.WriteLine("//" + GenerateProperty(f, true) + " // Implemented by user elsewhere");
                }
                else
                {
                    if (f.OptionDeprecated)
                        cw.WriteLine("[Obsolete]");
                    cw.WriteLine(GenerateProperty(f, true));
                }
                cw.WriteLine();
            }
        }

        private void WriteEquals(ProtoMessage m)
        {
            string equalsCast = (0 < m.Fields.Values.Count) ? m.CsType + " otherTyped = ("+m.CsType+") obj;" : "";

            cw.WriteLine(@"
public override bool Equals(object obj) {
    if (obj == null || !obj.GetType().Equals(GetType())) {
        return false;
    }
    " + equalsCast
                );
            foreach (Field f in m.Fields.Values)
            {
                cw.WriteLine("    if (!ProtobufUtil.IsEqual(_private._"+f.ProtoName+", otherTyped._private._" + f.ProtoName +")) return false;");
            }
            cw.WriteLine("    return true;");
            cw.WriteLine("}");
        }

        private void WriteGetHashCode(ProtoMessage m)
        {
            cw.WriteLine(@"
public override int GetHashCode() {
    int hash = 0;");

            foreach (Field f in m.Fields.Values)
            {
                cw.WriteLine("    hash = ProtobufUtil.CalcHash(_private._"+f.ProtoName+", hash);");
            }
            cw.WriteLine("    return hash;");
            cw.WriteLine("}");

            //Wire format field ID
#if DEBUGx
            cw.Comment("ProtocolBuffers wire field id");
            foreach (Field f in m.Fields.Values)
            {
                cw.WriteLine("public const int " + f.CsName + "FieldID = " + f.ID + ";");
            }
#endif
        }

        string GenerateProperty(Field f, bool accessor)
        {
            string type = f.ProtoType.FullCsType;
            if (f.OptionCodeType != null)
                type = f.OptionCodeType;
            if (f.Rule == FieldRule.Repeated)
                type = "ProtocolMessageList<" + type + ">";
            if (f.Rule == FieldRule.Optional && !f.ProtoType.Nullable && options.Nullable)
                type = type + "?";

            if (f.OptionReadOnly)
                return f.OptionAccess + " readonly " + type + " " + f.CsName + " = new " + type + "();";
            else if (f.ProtoType is ProtoMessage && f.ProtoType.OptionType == "struct")
                return f.OptionAccess + " " + type + " " + f.CsName + ";";
            else
            {
                if (!accessor)
                {
                    if (f.Rule == FieldRule.Repeated)
                    {
                        return "internal " + type + " _" + f.ProtoName + " = new ProtocolMessageList<" +
                               f.ProtoType.FullCsType + ">();\n";
                    }
                    else
                    {
                        return "internal " + type + " _" + f.ProtoName + ";\n";
                    }
                }
                else
                {
                    return f.OptionAccess + " " + type + " " + f.CsName + " { get {return _private._"+f.ProtoName+";} set { MarkDirty(); _private._"+f.ProtoName+" = value; } }";
                }

            }

        }
    }
}

