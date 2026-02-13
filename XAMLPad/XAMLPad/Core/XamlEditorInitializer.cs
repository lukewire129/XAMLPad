using Newtonsoft.Json;
using OpenSilver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace XAMLPad.Core;
public class OpenSilverCompletionDataCollector
{
    public class ControlInfo
    {
        public string Name { get; set; }
        public List<string> Properties { get; set; }
        public string Namespace { get; set; }
    }

    public List<ControlInfo> GetAllControls()
    {
        var controlInfos = new List<ControlInfo> ();

        // OpenSilver 어셈블리에서 컨트롤 타입 찾기
        var assemblies = new[]
        {
        typeof(Button).Assembly,  // System.Windows.Controls
        Assembly.GetExecutingAssembly()  // 내 프로젝트의 커스텀 컨트롤
    };

        foreach (var assembly in assemblies)
        {
            var controlTypes = assembly.GetTypes ()
                .Where (t => t.IsPublic &&
                            !t.IsAbstract &&
                            typeof (Control).IsAssignableFrom (t))
                .ToList ();

            foreach (var type in controlTypes)
            {
                var info = new ControlInfo
                {
                    Name = type.Name,
                    Namespace = type.Namespace,
                    Properties = GetDependencyProperties (type)
                };

                controlInfos.Add (info);
            }
        }

        return controlInfos;
    }

    private List<string> GetDependencyProperties(Type type)
    {
        var properties = new List<string> ();

        // DependencyProperty 필드 찾기
        var dpFields = type.GetFields (BindingFlags.Public | BindingFlags.Static)
            .Where (f => f.FieldType == typeof (DependencyProperty))
            .ToList ();

        foreach (var field in dpFields)
        {
            // "WidthProperty" -> "Width"
            var propName = field.Name.Replace ("Property", "");
            properties.Add (propName);
        }

        // 일반 속성도 추가
        var normalProps = type.GetProperties (BindingFlags.Public | BindingFlags.Instance)
            .Where (p => p.CanWrite)
            .Select (p => p.Name)
            .ToList ();

        properties.AddRange (normalProps);

        // 중복 제거
        return properties.Distinct ().OrderBy (p => p).ToList ();
    }

    // 공통 XAML 속성들
    public List<string> GetCommonXamlAttributes()
    {
        return new List<string>
        {
            "x:Name",
            "x:Key",
            "x:Class",
            "xmlns",
            "xmlns:x"
        };
    }
}
public class XamlEditorInitializer
{
    public void SetupAutoCompletion()
    {
        var collector = new OpenSilverCompletionDataCollector ();
        var controls = collector.GetAllControls ();
        var commonAttrs = collector.GetCommonXamlAttributes ();

        // JSON으로 변환
        var controlsJson = JsonConvert.SerializeObject (controls);
        var attrsJson = JsonConvert.SerializeObject (commonAttrs);

        string jsCode = $@"
(function() {{
    var controlsData = {controlsJson};
    var commonAttributes = {attrsJson};
    
    monaco.languages.registerCompletionItemProvider('xml', {{
        triggerCharacters: ['<', ' '],
        provideCompletionItems: function(model, position) {{
            var line = model.getLineContent(position.lineNumber);
            var prefix = line.substring(0, position.column - 1);
            
            // < 입력 시 컨트롤 제안
            if (prefix.endsWith('<')) {{
                return {{
                    suggestions: controlsData.map(function(ctrl) {{
                        return {{
                            label: ctrl.Name,
                            kind: monaco.languages.CompletionItemKind.Class,
                            insertText: ctrl.Name + ' $0></' + ctrl.Name + '>',
                            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                            documentation: 'Namespace: ' + ctrl.Namespace,
                            detail: ctrl.Properties.length + ' properties'
                        }};
                    }})
                }};
            }}
            
            // 태그 안에서 속성 제안
            var tagMatch = prefix.match(/<(\w+)\s+[^>]*$/);
            if (tagMatch) {{
                var tagName = tagMatch[1];
                
                // 해당 컨트롤의 속성 찾기
                var control = controlsData.find(c => c.Name === tagName);
                var attrs = commonAttributes.slice(); // 공통 속성 먼저
                
                if (control) {{
                    attrs = attrs.concat(control.Properties);
                }}
                
                return {{
                    suggestions: attrs.map(function(attr) {{
                        return {{
                            label: attr,
                            kind: monaco.languages.CompletionItemKind.Property,
                            insertText: $@""`{{attr}}=""""$1""""$0`"",
                            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet
                        }};
                    }})
                }};
            }}
            
            return {{ suggestions: [] }};
        }}
    }});
}})();
";

        Interop.ExecuteJavaScript (jsCode);
    }
}