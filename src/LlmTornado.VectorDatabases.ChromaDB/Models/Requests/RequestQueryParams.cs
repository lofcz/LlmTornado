using System.Collections;
using System.Globalization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models.Requests;

internal class RequestQueryParams : IEnumerable<(string key, string value)>
{
	private readonly Dictionary<string, string> _queryParams;

	public RequestQueryParams()
	{
		_queryParams = new Dictionary<string, string>(StringComparer.Ordinal);
	}

	public IEnumerator<(string key, string value)> GetEnumerator()
		=> _queryParams
			.Select(kvp => (kvp.Key, kvp.Value))
			.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public RequestQueryParams Insert(string key, string value)
	{
		_queryParams[key] = value;
		return this;
	}
	public RequestQueryParams Insert(string key, IFormattable value)
		=> Insert(key, value.ToString(null, CultureInfo.InvariantCulture));
}
