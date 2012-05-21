using System;
using System.Collections.Generic;
using System.Linq;

namespace DictionarySpeedTests.StringNormalisers
{
    /// <summary>
    /// This will combine the effects of multiple string normalisers - each will be applied in turn, in the order in which they are specified to the
    /// constructor. This would allow - for example - the DefaultStringNormaliser to be used to any non-latin characters before passing the value
    /// on to the EnglishPluarityStringNormaliser.
    /// </summary>
    [Serializable]
    public class CombinedStringNormaliser : StringNormaliser
    {
        private List<IStringNormaliser> _stringNormalisers;
        public CombinedStringNormaliser(IEnumerable<IStringNormaliser> stringNormalisers)
        {
            if (stringNormalisers == null)
                throw new ArgumentNullException("stringNormalisers");

            var stringNormalisersTidied = new List<IStringNormaliser>();
            foreach (var stringNormaliser in stringNormalisers)
            {
                if (stringNormaliser == null)
                    throw new ArgumentException("Null reference encountered in stringNormalisers set");
                stringNormalisersTidied.Add(stringNormaliser);
            }
            if (!stringNormalisersTidied.Any())
                throw new ArgumentException("Zero entries in stringNormalisers set");

            _stringNormalisers = stringNormalisersTidied;
        }

        public override string GetNormalisedString(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            foreach (var stringNormaliser in _stringNormalisers)
                value = stringNormaliser.GetNormalisedString(value);

            return value;
        }
    }
}
