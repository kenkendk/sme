using System;
using System.Collections.Generic;

namespace SME.AST
{
	public static class JsonHelpers
	{
		public static string ToJson(this Method method)
		{
			using (var ms = new System.IO.MemoryStream())
			{
				ToJson(method, ms);
				return System.Text.Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
			}
		}

		public static void ToJson(this Method method, System.IO.Stream target)
		{
			using (var sw = new System.IO.StreamWriter(target, System.Text.Encoding.UTF8, 4 * 1024, true))
			using (var jw = new Newtonsoft.Json.JsonTextWriter(sw))
			{
				jw.Formatting = Newtonsoft.Json.Formatting.Indented;

				foreach (var _ in method.All((item, action) =>
				{
					if (item == method)
					{
						if (action == VisitorState.Enter)
							jw.WriteStartArray();
						else if (action == VisitorState.Leave)
							jw.WriteEndArray();

						return true;
					}
					else if (item is Statement)
					{
						if (action == VisitorState.Enter)
						{
							var stm = item as Statement;

							jw.WriteStartObject();

							jw.WritePropertyName("type");
							jw.WriteValue(stm.GetType().FullName);

							if (!string.IsNullOrWhiteSpace(stm.Name))
							{
								jw.WritePropertyName("name");
								jw.WriteValue(stm.Name);
							}

							if (stm.SourceStatement != null)
							{
								jw.WritePropertyName("source");
								jw.WriteValue(stm.SourceStatement.ToString());
							}

							jw.WritePropertyName("children");
							jw.WriteStartArray();
						}
						else if (action == VisitorState.Leave)
						{
							jw.WriteEndArray();
							jw.WriteEndObject();
						}

						return true;
					}
					else if (item is Expression)
					{
						if (action == VisitorState.Enter)
						{
							var expr = item as Expression;

							jw.WriteStartObject();

							jw.WritePropertyName("type");
							jw.WriteValue(expr.GetType().FullName);

							if (!string.IsNullOrWhiteSpace(expr.Name))
							{
								jw.WritePropertyName("name");
								jw.WriteValue(expr.Name);
							}

							if (expr.SourceExpression != null)
							{
								jw.WritePropertyName("source");
								jw.WriteValue(expr.SourceExpression.ToString());
							}

							jw.WritePropertyName("children");
							jw.WriteStartArray();
						}
						else if (action == VisitorState.Leave)
						{
							jw.WriteEndArray();
							jw.WriteEndObject();
						}

						return true;
					}
					else
						return false;
				}))
				{ }
			}
		}
	}
}
