using System;
namespace JSON.Entities.Exceptions
{
	abstract public class JsonExceptions: Exception
	{
		
	}

	sealed public class NullValueException: JsonExceptions {

	}
}

