using CSHTML5.Native.Html.Controls;
using Newtonsoft.Json;
using OpenSilver;
using System;
using System.Windows;
using XAMLPad.Core;

namespace XAMLPad.Components
{
    public class MonacoEditor : HtmlPresenter
    {
        private const string CdnUrl = "https://unpkg.com/monaco-editor@latest";
        private const string IframeId = "monacoEditorIframe";

        private object _domElement;

        #region Code
        public string Code
        {
            get => (string)GetValue (CodeProperty);
            set => SetValue (CodeProperty, value);
        }

        public static readonly DependencyProperty CodeProperty =
            DependencyProperty.Register (nameof (Code), typeof (string), typeof (MonacoEditor), new PropertyMetadata (string.Empty, OnCodeChanged));

        private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (MonacoEditor)d;
            if (editor._domElement != null)
            {
                Interop.ExecuteJavaScriptVoid ($"document.getElementById('{IframeId}').contentWindow.setEditorContent(`{e.NewValue}`)");
            }
        }
        #endregion

        #region Language
        public new string Language
        {
            get => (string)GetValue (LanguageProperty);
            set => SetValue (LanguageProperty, value);
        }

        public static new readonly DependencyProperty LanguageProperty =
            DependencyProperty.Register (nameof (Language), typeof (string), typeof (MonacoEditor), new PropertyMetadata ("plaintext", OnLanguageChanged));

        private static void OnLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (MonacoEditor)d;
            if (editor._domElement != null)
            {
                Interop.ExecuteJavaScriptVoid ($"document.getElementById('{IframeId}').contentWindow.setEditorLanguage('{e.NewValue}')");
            }
        }
        #endregion

        public MonacoEditor()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            _domElement = Interop.GetDiv (this);
            Language = "xml";
            // 1. 컨트롤 정보 수집
            var collector = new OpenSilverCompletionDataCollector ();
            var controls = collector.GetAllControls ();
            var commonAttrs = collector.GetCommonXamlAttributes ();

            var controlsJson = JsonConvert.SerializeObject (controls);
            var attrsJson = JsonConvert.SerializeObject (commonAttrs);
            // 2. JavaScript에 포함
            Interop.ExecuteJavaScriptVoidAsync ($$"""
            (function () {
                const iframe = document.createElement('iframe');
                iframe.setAttribute('id','{{IframeId}}');
                iframe.style='border:none;width:100%;height:100%;overflow:hidden;display:block';
 
                const currentDiv = $0;
                currentDiv.appendChild(iframe);
 
                const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
                iframeDoc.open();
                const div = `
                    <html>
                        <style>
                            body, html {
                                margin: 0;
                                padding: 0;
                                width: 100%;
                                height: 100%;
                                overflow:hidden;
                            }
                            #monacoContainer {
                                width: 100%;
                                height: 100%;
                                margin: 0;
                                padding: 0;
                            }
                        </style>
                        <body>
                            <div id="monacoContainer"></div>
                        </body>
                    </html>
                `;
                iframeDoc.write(div);
 
                const monacoLoaderScript = iframeDoc.createElement('script');
                monacoLoaderScript.src = '{{CdnUrl}}/min/vs/loader.js';
                monacoLoaderScript.onload = function()
                {
                    var configScript = iframeDoc.createElement('script');
                    configScript.type = 'text/javascript';
                    configScript.text = `
                        require.config({ paths: { 'vs': '{{CdnUrl}}/min/vs' } });
                        require(['vs/editor/editor.main'], function() {
                            
                            // ========== 자동완성 설정 시작 ==========
                            var controlsData = {{controlsJson}};
                            var commonAttributes = {{attrsJson}};
                            
                            monaco.languages.registerCompletionItemProvider('xml', {
                                triggerCharacters: ['<', ' '],
                                provideCompletionItems: function(model, position) {
                                    var lineContent = model.getLineContent(position.lineNumber);
                                    var textBeforeCursor = lineContent.substring(0, position.column - 1);

                                    // < 입력 시 컨트롤 제안
                                    if (textBeforeCursor.endsWith('<')) {
                                        return {
                                            suggestions: controlsData.map(function(ctrl) {
                                                return {
                                                    label: ctrl.Name,
                                                    kind: monaco.languages.CompletionItemKind.Class,
                                                    // 수정: 커서 위치를 태그명 뒤로 이동
                                                    insertText: ctrl.Name + ' ' + String.fromCharCode(36) + '0></' + ctrl.Name,
                                                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                                                    documentation: ctrl.Namespace
                                                };
                                            })
                                        };
                                    }

                                    // 속성 제안
                                    var tagMatch = textBeforeCursor.match(/<(\\w+)\\s+[^>]*$/);
                                    if (tagMatch) {
                                        var tagName = tagMatch[1];
                                        var control = controlsData.find(c => c.Name === tagName);
                                        var attrs = commonAttributes.slice();

                                        if (control && control.Properties) {
                                            attrs = attrs.concat(control.Properties);
                                        }

                                        return {
                                            suggestions: attrs.map(function(attr) {
                                                return {
                                                    label: attr,
                                                    kind: monaco.languages.CompletionItemKind.Property,
                                                    insertText: attr + '="' + String.fromCharCode(36) + '1"' + String.fromCharCode(36) + '0',
                                                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet
                                                };
                                            })
                                        };
                                    }
                                    
                                    return { suggestions: [] };
                                }
                            });
                            // ========== 자동완성 설정 끝 ==========
                            
                            // 에디터 생성
                            let element=document.getElementById('monacoContainer');
                            element.editor = monaco.editor.create(element, {
                                    value: \`{{Code}}\`,
                                    language: '{{Language}}',
                                    theme: 'vs-dark',
                                    automaticLayout: true,
                                    scrolling: { vertical:'auto' }
                            });
                            element.style.height = '100%';
                            element.editor.onDidChangeModelContent((event) => {
                                const fullContent = element.editor.getValue();
                                window.updateContentInParent(fullContent);
                            });
                        });
                    `;
                    iframeDoc.body.appendChild(configScript);
                };
                iframeDoc.body.appendChild(monacoLoaderScript);
 
                const scriptCallback = iframeDoc.createElement('script');
                scriptCallback.text = `
                        window.setEditorContent = function(message) {
                            let element=document.getElementById('monacoContainer');
                            element && element.editor && element.editor.getModel().getValue() != message && element.editor.getModel().setValue(message)
                        };
                        window.setEditorLanguage = function(language) {
                            let editor = document.getElementById('monacoContainer')?.editor;
                            if (editor) {
                                monaco.editor.setModelLanguage(editor.getModel(), language);
                            }
                        };
                `;
 
                iframeDoc.body.appendChild(scriptCallback);
                iframeDoc.close();
                const cw = document.getElementById('{{IframeId}}').contentWindow;
                cw.updateContentInParent = function(message) {
                    $2(message);
                };
            })();
""", _domElement, Language, (Action<string>)OnCodeChangedInEditor);
        }

        private void OnCodeChangedInEditor(string newCode)
        {
            Code = newCode;
        }
    }
}
