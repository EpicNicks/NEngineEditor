﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NEngineEditor.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("NEngineEditor.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap CSharpScriptIcon {
            get {
                object obj = ResourceManager.GetObject("CSharpScriptIcon", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;Project Sdk=&quot;Microsoft.NET.Sdk&quot;&gt;
        ///  &lt;PropertyGroup&gt;
        ///    &lt;OutputType&gt;Exe&lt;/OutputType&gt;
        ///    &lt;TargetFramework&gt;net8.0&lt;/TargetFramework&gt;
        ///    &lt;Nullable&gt;enable&lt;/Nullable&gt;
        ///    &lt;StartupObject&gt;Program&lt;/StartupObject&gt;
        ///  &lt;/PropertyGroup&gt;
        ///
        ///  &lt;ItemGroup&gt;
        ///    &lt;Reference Include=&quot;NEngine&quot;&gt;
        ///      &lt;HintPath&gt;.Engine\NEngine.dll&lt;/HintPath&gt;
        ///    &lt;/Reference&gt;
        ///  &lt;/ItemGroup&gt;
        ///
        ///  &lt;ItemGroup&gt;
        ///    &lt;PackageReference Include=&quot;SFML.Net&quot; Version=&quot;2.6.0&quot; /&gt;
        ///  &lt;/ItemGroup&gt;
        ///
        ///  &lt;ItemGroup&gt;
        ///    &lt;!-- Add any other dependencies [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CsProjTemplate_csproj {
            get {
                return ResourceManager.GetString("CsProjTemplate_csproj", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap ellipsis_horizontal {
            get {
                object obj = ResourceManager.GetObject("ellipsis_horizontal", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap FolderIcon {
            get {
                object obj = ResourceManager.GetObject("FolderIcon", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System.Collections.Generic;
        ///
        ///using SFML.Graphics;
        ///
        ///using NEngine.GameObjects;
        ///
        ///// change &quot;GameObject&quot; to &quot;Positionable&quot; if you need your GameObject to have a position or &quot;Moveable&quot; to handle movement with its Move function in Update
        ///public class {CLASSNAME} : GameObject
        ///{
        ///    // The general setup for your SFML Drawables. Store references in this list.
        ///    public override List&lt;Drawable&gt; Drawables { get; set; } = [];
        ///
        ///    // where you should add your Drawables to the Drawables list
        ///    publ [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string GameObjectTemplate_cs {
            get {
                return ResourceManager.GetString("GameObjectTemplate_cs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to // Implicit Usings not enabled in generated projects
        ///using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///
        ///using System.Reflection;
        ///using System.Globalization;
        ///using System.IO;
        ///using System.Text.Json;
        ///using System.Text.RegularExpressions;
        ///
        ///using SFML.System;
        ///using SFML.Graphics;
        ///
        ///using NEngine;
        ///using NEngine.Window;
        ///using NEngine.GameObjects;
        ///
        ////// &lt;summary&gt;
        ////// Based in the root directory of your project.
        ////// Runs the scenes created here along with your config files in the  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ProjectProgram {
            get {
                return ResourceManager.GetString("ProjectProgram", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap SceneIcon {
            get {
                object obj = ResourceManager.GetObject("SceneIcon", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
    }
}
