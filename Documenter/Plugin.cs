using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UDIMAS;

namespace Documenter
{
    public class Documenter : UdimasExternalPlugin
    {
        public override string Name => "Documenter";

        public override void Run()
        {
            CmdInterpreter.RegisterCommand(new TerminalCommand("docs", Cmd));
        }
        private (int, string) Cmd(InterpreterIOPipeline tw, string[] args)
        {
            if (CmdInterpreter.IsWellFormatterArguments(args, "-h|--help"))
            {
                CmdInterpreter.PrintLines(tw, new string[] {
                    "generates syntax documentation. ",
                    "Documentation is written into /docs/ folder.",
                    "Usage:",
                    " docs filter\tfilters namespaces with supplied filter. Wildcards supported",
                    " docs *\t\tgenerates documentation of ALL .NET framework and other items. TAKES A LONG TIME"
                });
            }
            else if (CmdInterpreter.IsWellFormatterArguments(args, "\\S+"))
            {
                //generate documentation
                Generate(tw, args[0]);
            }
            else return (1, "Invalid arguments. -h for help");

            return (0, "");
        }

        private void Generate(InterpreterIOPipeline console, string filter)
        {
            //declare directory string before local functions so it can be used in them
            string dir = Path.Combine(Udimas.SystemDirectory, "docs");

            //Documents a type and all items (such as methods and properties) inside it
            void IterateTypeInfo(TypeInfo ti, int tabs, StreamWriter stream = null)
            {
                StreamWriter sw = stream ?? new StreamWriter(Path.Combine(dir, ti.Namespace + ".txt"), true);

                //used if documenting a class inside a class
                string tabstr = new string('\t', tabs);

                sw.Write(ti.AsType().FormatStatic() + ti.Name);

                // generic = class myclass<T> { }
                //                         ^ uses generic types and is generic
                if (ti.IsGenericType)
                {
                    sw.Write(ti.AsType().FormatGenericParameters());
                }

                // if it's a delegate, write its parameters
                if (typeof(MulticastDelegate).IsAssignableFrom(ti.BaseType))
                {
                    sw.Write($"({ti.GetMethod("Invoke").GetParameters().FormatParameters()})");
                }

                // or if it's a class which has constructors, write its all possible constructors
                else if (ti.IsClass && ti.GetConstructors().Length > 0)
                {
                    bool first = true;
                    foreach (var ci in ti.GetConstructors())
                    {
                        if (!first) sw.Write("\r\n" + tabstr + ti.Name);
                        else first = false;

                        if (ci.IsPublic) // write only public constructors
                        {
                            sw.Write($"({ci.GetParameters().FormatParameters()})");
                        }
                    }
                }
                sw.WriteLine();

                //these method names are ignored
                string[] ignore =
                {
                "GetType",
                "ToString",
                "Equals",
                "GetHashCode"
                };
                var members = ti.AsType().GetMembers()
                    .Where(x =>
                        (x.MemberType == MemberTypes.Method && !((MethodInfo)x).IsSpecialName && !ignore.Contains(x.Name))
                        || (x.MemberType != MemberTypes.Constructor && x.MemberType != MemberTypes.Method)
                    );

                //iterate through all members
                foreach (MemberInfo item in members)
                {
                    sw.Write("\t" + tabstr);
                    switch (item.MemberType)
                    {
                        case MemberTypes.Method:
                            sw.WriteLine(((MethodInfo)item).MethodSignature());
                            break;
                        case MemberTypes.Property: // properties have getters and/or setters..
                            var p = (PropertyInfo)item;
                            sw.WriteLine(p.PropertySignature());
                            break;
                        case MemberTypes.Field: //.. fields do not
                            var f = (FieldInfo)item;
                            sw.WriteLine(f.FormatStatic() + f.FieldType.Name + " " + f.Name);
                            break;
                        case MemberTypes.TypeInfo: // possibly never occurs, just in case
                            IterateTypeInfo((TypeInfo)item, tabs + 1, sw);
                            break;
                        case MemberTypes.NestedType:
                            IterateTypeInfo((TypeInfo)item, tabs + 1, sw);
                            break;
                        case MemberTypes.Event:
                            var e = (EventInfo)item;
                            sw.WriteLine($"{e.FormatStatic()}event {e.EventHandlerType.TypeName() + e.EventHandlerType.FormatGenericArguments()} {e.Name}");
                            break;
                    }
                }
                sw.Flush();
                if (stream == null)
                {
                    //if nothing was written to file, delete it so there is no empty files generated
                    long len = sw.BaseStream.Length;
                    sw.Close();
                    if (len == 0) File.Delete(Path.Combine(dir, ti.Namespace + ".txt"));
                }
            }

            //converts a wildcard string to a regex string
            String WildCardToRegular(String value)
            {
                return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
            }

            console.Write("Gathering information.. ");

            List<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<TypeInfo> typeinfos = new List<TypeInfo>();
            var regString = WildCardToRegular(filter);

            assemblies.ForEach(a => {
                typeinfos.AddRange(
                    a.DefinedTypes.Where(x =>
                        !string.IsNullOrWhiteSpace(x.Namespace) &&
                        x.Namespace.IndexOfAny("<>".ToCharArray()) == -1 &&
                        Regex.IsMatch(x.Namespace, regString) && 
                        x.IsPublic //do not document private/internal things
                        )
                        .Distinct()
                    );
                
            });

            //delete all existing files (may cause unnecessary enumeration)
            typeinfos
                .Select(x => x.Namespace)
                .Distinct()
                .ToList()
                .ForEach(x => { if (File.Exists(Path.Combine(dir, x + ".txt"))) File.Delete(Path.Combine(dir, x + ".txt")); });

            console.WriteLine("Done");
            console.WriteLine($"Documenting {typeinfos.Count} types with filter '{filter}' from {assemblies.Count} loaded assemblies");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            try
            {
                int count = 0;
                string str = "";
                foreach (TypeInfo ti in typeinfos)
                {
                    //delete previously written text
                    console.Write(new string('\b', str.Length));
                    count++;
                    str = string.Format("{0}/{1} ({2}%)", 
                        count,
                        typeinfos.Count,
                        Math.Round(((decimal)count / (decimal)typeinfos.Count) * (decimal)100));
                    console.Write(str);

                    IterateTypeInfo(ti, 0);
                }
                console.WriteLine();
            }
            catch (Exception e)
            {
                console.WriteLine();
                console.WriteLine(e.Message);
            }
        }
    }

    /// <summary>
    /// These methods helps in formatting in reflection types
    /// </summary>
    internal static class Helpers
    {
        public static string MethodSignature(this MethodInfo mi)
        {
            string signature = 
                String.Format("{5}{0}{4} {1}{2}({3})", 
                    mi.ReturnType.TypeName(), mi.Name, mi.GetGenericArguments().FormatGenericParameters(), 
                    mi.GetParameters().FormatParameters(), mi.ReturnType.FormatGenericArguments(),
                    mi.FormatStatic()
                    );

            return signature;
        }
        public static string PropertySignature(this PropertyInfo pi)
        {
            string signature =
                String.Format("{4}{0}{2} {1} {{{3}}}",
                    pi.PropertyType.TypeName(), pi.Name, pi.PropertyType.FormatGenericArguments(),
                    (pi.CanRead && pi.GetMethod.IsPublic ? " get;" : "") + (pi.CanWrite && pi.SetMethod.IsPublic ? " set;" : "") + " ",
                    pi.FormatStatic()
                    );

            return signature;
        }
        public static string FormatGenericArguments(this Type t)
        {
            if (t.IsGenericType)
            {
                return "<" + string.Join(", ", t.GetTypeInfo().GenericTypeArguments.Select(x => x.Name)) + ">";
            }
            return "";
        }
        public static string FormatGenericParameters(this Type t)
        {
            if (t.IsGenericType)
            {
                return "<" + string.Join(", ", t.GetTypeInfo().GenericTypeParameters.Select(x => x.Name)) + ">";
            }
            return "";
        }
        public static string FormatGenericParameters(this Type[] parameters)
        {
            if (parameters.Length > 0)
                return "<" + string.Join(", ", parameters.Select(x => x.Name)) + ">";
            else return "";
        }

        public static string FormatParameters(this ParameterInfo[] par)
        {
            string GetTypeString(Type t)
            {
                string r = t.IsGenericParameter ? t.Name : t.TypeName();
                r += t.FormatGenericArguments();
                return r;
            }
            return string.Join(", ", 
                          par.Select(p => String.Format("{0} {1}", GetTypeString(p.ParameterType), p.Name))
                          );
        }
        public static string TypeName(this Type t)
        {
            return $"{t.Namespace}.{t.Name}";
        }
        public static string FormatStatic(this Type t)
        {
            return (t.IsAbstract && t.IsSealed) ? "static " : "";
        }
        public static string FormatStatic(this MethodInfo t)
        {
            return (t.IsStatic) ? "static " : "";
        }
        public static string FormatStatic(this PropertyInfo t)
        {
            return (t.GetGetMethod()?.IsStatic??false) ? "static " : "";
        }
        public static string FormatStatic(this FieldInfo t)
        {
            return (t.IsStatic) ? "static " : "";
        }
        public static string FormatStatic(this EventInfo e)
        {
            return (e.GetAddMethod().IsStatic) ? "static " : "";
        }
    }
}
