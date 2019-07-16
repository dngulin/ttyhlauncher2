using NGettext;

namespace TtyhLauncher.Localization {
    public class Translator {
        private readonly Catalog _catalog;
        
        public Translator(Catalog catalog) {
            _catalog = catalog;
        }

        public string _(string text)
        {
            return _catalog.GetString(text);
        }

        public string _(string text, params object[] args)
        {
            return _catalog.GetString(text, args);
        }

        public string _n(string text, string pluralText, long n)
        {
            return _catalog.GetPluralString(text, pluralText, n);
        }

        public string _n(string text, string pluralText, long n, params object[] args)
        {
            return _catalog.GetPluralString(text, pluralText, n, args);
        }

        public string _p(string context, string text)
        {
            return _catalog.GetParticularString(context, text);
        }

        public string _p(string context, string text, params object[] args)
        {
            return _catalog.GetParticularString(context, text, args);
        }

        public string _pn(string context, string text, string pluralText, long n)
        {
            return _catalog.GetParticularPluralString(context, text, pluralText, n);
        }

        public string _pn(string context, string text, string pluralText, long n, params object[] args)
        {
            return _catalog.GetParticularPluralString(context, text, pluralText, n, args);
        }
    }
}