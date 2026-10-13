using Umbraco.Cms.Core.PropertyEditors;

namespace Webwonders.Baseline.Meta.PropertyEditors;

/// <summary>
/// Property editor for authoring llms.txt content (title, description and chapters of links).
/// Editors create a data type from it in Settings → Data Types, so Umbraco Deploy treats it as normal schema.
/// The value is stored as JSON, see <see cref="Models.LlmsTxtModel"/>.
/// </summary>
[DataEditor(Constants.LlmsTxt.EditorAlias, ValueType = ValueTypes.Json, ValueEditorIsReusable = true)]
public class LlmsTxtPropertyEditor(IDataValueEditorFactory dataValueEditorFactory) : DataEditor(dataValueEditorFactory)
{
}
