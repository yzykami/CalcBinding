﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace CalcBinding.PathAnalysis
{
    public class PropertyPathAnalyzer
    {
        #region private fields
        public static string[] UnknownDelimiters = new[] 
            { 
                "(", ")", "+", "-", "*", "/", "%", "^", "&&", "||", 
                "&", "|", "?", "<=", ">=", "<", ">", "==", "!=", "!", "," 
            };

        public static string[] KnownDelimiters = new[]
            {
                ".", ":"
            };

        private static string[] delimiters;

        //private int _position;
        //private State _state;
        //private SubState _subState;
        //private string _str;

        //private List<PathToken> _pathTokens;
        //private Token namespaceIdentifier;
        //private Token classIdentifier;
        //private List<Token> propertyPathIdentifiers;
        //private Token _lastIdentifier;

        //private int _identifierStartPos;
        private IXamlTypeResolver _typeResolver;
        #endregion


        #region Static constructor

        static PropertyPathAnalyzer()
        {
            delimiters = KnownDelimiters.Concat(UnknownDelimiters).ToArray();
        } 

        #endregion


        #region Parser cycle

        public List<PathToken> GetPathes(string normPath, IXamlTypeResolver typeResolver)
        {
            _typeResolver = typeResolver;
            //_state = State.Initial;
            //_position = 0;
            //_pathTokens = new List<PathToken>();
            //_str = normPath;
            //propertyPathIdentifiers = new List<Token>();

            Trace.WriteLine(string.Format("PropertyPathAnalyzer.GetPathes: start read {0} ", normPath));

            var chunks = GetChunks(normPath);

            var pathes = GetPathes(chunks);

            //Trace.WriteLine(string.Format("PropertyPathAnalyzer.GetPathes: end read {0} ", normPath));
            //Trace.WriteLine(string.Format("PropertyPathAnalyzer.GetPathes: tokens: {0}", String.Join("", pathes.Select(pt => string.Format("\n{0} ({1})", pt.Id.Value, pt.Id.PathType)))));

            return pathes;
        }

        private List<PathToken> GetPathes(List<Chunk> chunks)
        {
            List<PathToken> tokens = new List<PathToken>();

            foreach (var chunk in chunks)
            {
                PathToken path;

                if (GetPath(chunk, out path))
                {
                    TracePath(path);
                    tokens.Add(path);
                }
            }

            return tokens;
        }

        private bool GetPath(Chunk chunk, out PathToken pathToken)
        {
            var str = (string)chunk.Value;

            var colonPos = str.IndexOf(':');

            if (colonPos > 0)
            {
                var left = SubStr(str, 0, colonPos - 1);

                if (IsIdentifier(left))
                {
                    List<string> propChain;
                    if (GetPropChain(SubStr(str, colonPos+1, str.Length-1), out propChain))
                    {
                        if (propChain.Count() > 1)
                        {
                            pathToken = GetEnumOrStaticProperty(chunk, left, propChain);
                            return true;
                        }
                    }
                }
            }
            else
            {
                List<string> propChain;
                if (GetPropChain(str, out propChain))
                {
                    pathToken = GetPropPathOrMath(chunk, propChain);
                    return true;
                }
            }

            pathToken = null;
            return false;
        }

        private PathToken GetPropPathOrMath(Chunk chunk, List<string> propChain)
        {
            PathToken pathToken = null;

            if (propChain.Count() == 2 && propChain[0] == "Math")
            {
                pathToken = new MathToken(chunk.Start, chunk.End, propChain[1]);
            }
            else
            {
                pathToken = new PropertyPathToken(chunk.Start, chunk.End, propChain);
            }

            return pathToken;
        }

        private PathToken GetEnumOrStaticProperty(Chunk chunk, string @namespace, List<string> identifierChain)
        {
            PathToken pathToken = null;
            Type enumType;
            var className = identifierChain[0];
            string fullClassName = string.Format("{0}:{1}", @namespace, className);

            var propertyChain = identifierChain.Skip(1).ToList();
            if (propertyChain.Count == 1 && ((enumType = TakeEnum(fullClassName)) != null))
            {
                // enum output
                var enumMember = propertyChain.Single();
                pathToken = new EnumToken(chunk.Start, chunk.End, @namespace, enumType, enumMember);
            }
            else
            {
                //static property path output
                pathToken = new StaticPropertyPathToken(chunk.Start, chunk.End, @namespace, className, propertyChain);
            }
            return pathToken;
        }

        private bool GetPropChain(string str, out List<string> propChain)
        {
            var properties = str.Split(new[] {'.'}, StringSplitOptions.None);

            if (properties.All(IsIdentifier) && properties.Any())
            {
                propChain = properties.ToList();
                return true;
            }

            propChain = null;
            return false;
        }

        private bool IsIdentifier(string str)
        {
            if (str.Length == 0)
                return false;

            var firstChar = str[0];

            if (Char.IsDigit(firstChar) || delimiters.Contains(firstChar.ToString()))
                return false;

            for (int i = 1; i <= str.Length - 1; i++)
                if (delimiters.Contains(str[i].ToString()))
                    return false;

            return true;
        }

        private List<Chunk> GetChunks(string str)
        {
            int chunkStart = 0;
            var isChunk = false;
            List<Chunk> chunks = new List<Chunk>();
            int position = 0;

            throw new NotImplementedException("строки, строки забыли!");
            do
            {
                var c = position >= str.Length ? (char)0 : str[position];

                var isDelim = UnknownDelimiters.Contains(c.ToString()) || c == 0;

                if (isChunk)
                {
                    if (isDelim)
                    {
                        chunks.Add(new Chunk(SubStr(str, chunkStart, position - 1), chunkStart, position - 1));
                        isChunk = false;
                    }
                }
                else
                {
                    if (!isDelim)
                    {
                        chunkStart = position;
                        isChunk = true;
                    }
                }

                if (c == 0)
                    return chunks;

                position++;

            } while (true);
        }

        //private bool NextStep(Token token)
        //{
        //    switch (_state)
        //    {
        //        case State.Initial:
        //            {
        //                if (token.IsIdentifier && token.Value == "Math")
        //                {
        //                    _lastIdentifier = token;
        //                    _state = State.MathClass;
        //                    return true;
        //                }
        //                else if (token.IsIdentifier)
        //                {
        //                    _lastIdentifier = token;
        //                    _state = State.Identifier;
        //                    return true;
        //                }
        //                else if (token.IsEmpty)
        //                    return true;

        //                return false;
        //            }
        //        case State.MathClass:
        //            {
        //                if (token.IsDot)
        //                {
        //                    token = ReadNextToken();

        //                    if (token.IsIdentifier)
        //                    {
        //                        // math member output
        //                        var mathToken = new MathToken(_lastIdentifier.Start, token.End, token.Value);

        //                        TraceToken(mathToken);
        //                        _pathTokens.Add(mathToken);

        //                        _state = State.Initial;
        //                        return true;
        //                    }
        //                }
        //                return false;
        //            }
        //        case State.Identifier:
        //            {
        //                if (token.IsColon)
        //                {
        //                    namespaceIdentifier = _lastIdentifier;

        //                    token = ReadNextToken();

        //                    if (token.IsIdentifier)
        //                    {
        //                        classIdentifier = token;

        //                        token = ReadNextToken();

        //                        if (token.IsDot)
        //                        {
        //                            _state = State.StaticPropPathDot;
        //                            return true;
        //                        }
        //                    }
        //                }
        //                else if (token.IsDot)
        //                {
        //                    propertyPathIdentifiers.Add(_lastIdentifier);
        //                    _state = State.PropPathDot;
        //                    return true;
        //                }
        //                else
        //                {
        //                    propertyPathIdentifiers.Add(_lastIdentifier);
        //                    var firstIdentifier = propertyPathIdentifiers.First();
        //                    // property path output
        //                    var propPathToken = new PropertyPathToken(firstIdentifier.Start, _lastIdentifier.End, propertyPathIdentifiers.Select(i => i.Value));
        //                    TraceToken(propPathToken);
        //                    _pathTokens.Add(propPathToken);
        //                    propertyPathIdentifiers.Clear();
        //                    _state = State.Initial;
        //                    return true;
        //                }
        //                return false;
        //            }
        //        case State.StaticPropPathDot:
        //            {
        //                if (token.IsIdentifier)
        //                {
        //                    propertyPathIdentifiers.Add(token);
        //                    _lastIdentifier = token;

        //                    token = ReadNextToken();

        //                    if (token.IsDot)
        //                    {
        //                        // state unchanged
        //                        return true;
        //                    }
        //                    else
        //                    {
        //                        PathToken pathToken;
        //                        Type enumType;
        //                        string typeFullName = SubStr(namespaceIdentifier.Start, classIdentifier.End);
        //                        if (propertyPathIdentifiers.Count == 1 && ((enumType = TakeEnum(typeFullName)) != null))
        //                        {
        //                            // enum output
        //                            var enumMember = propertyPathIdentifiers.Single();
        //                            pathToken = new EnumToken(namespaceIdentifier.Start, enumMember.End, namespaceIdentifier.Value, enumType, enumMember.Value);
        //                        }
        //                        else
        //                        {
        //                            //static property path output
        //                            pathToken = new StaticPropertyPathToken(namespaceIdentifier.Start, _lastIdentifier.End, namespaceIdentifier.Value, classIdentifier.Value, propertyPathIdentifiers.Select(i => i.Value));
        //                        }
        //                        TraceToken(pathToken);
        //                        _pathTokens.Add(pathToken);

        //                        propertyPathIdentifiers.Clear();
        //                        _state = State.Initial;
        //                        return true;
        //                    }
        //                }
        //                return false;
        //            }
        //        case State.PropPathDot:
        //            {
        //                if (token.IsIdentifier)
        //                {
        //                    token = ReadNextToken();

        //                    if (token.IsDot)
        //                    {
        //                        // state unchanged
        //                        return true;
        //                    }
        //                    else
        //                    {
        //                        // property path output
        //                        var propPathToken = new PropertyPathToken(token.Start, token.End, propertyPathIdentifiers.Select(i => i.Value));
        //                        TraceToken(propPathToken);
        //                        _pathTokens.Add(propPathToken);
        //                        propertyPathIdentifiers.Clear();
        //                        _state = State.Initial;
        //                        return true;
        //                    }
        //                }
        //                return false;
        //            }
        //        default:
        //            throw new NotSupportedException(String.Format("PropertyPathAnalyzer: State {0} is not supported", _state));
        //    }
        //}

        private void TracePath(PathToken path)
        {
            Trace.WriteLine(string.Format("PropertyPathAnalyzer: read {0} ({1}) ({2}-{3})", path.Id.Value, path.Id.PathType, path.Start, path.End));
        }

        #endregion


        //#region SubParser cycle

        //private Token ReadNextToken()
        //{
        //    _subState = SubState.Initial;

        //    while (true)
        //    {
        //        Symbol symbol = _position < _str.Length ? (Symbol)_str[_position] : Symbol.End;
        //        var token = ReadToken(symbol);

        //        if (token != null || symbol.IsEnd)
        //        {
        //            var resultToken = token ?? Token.Empty(_position);
        //            Trace.WriteLine(string.Format("PropertyPathAnalyzer: read {0} ({1})", resultToken.Value, resultToken.TokenType));
        //            return resultToken;
        //        }
        //        _position++;
        //    }
        //}

        //private Token ReadToken(Symbol symbol)
        //{
        //    switch (_subState)
        //    {
        //        case SubState.Initial:

        //            if (symbol == '.')
        //            {
        //                _position++;
        //                return Token.Dot(_position);
        //            }

        //            if (symbol == ':')
        //            {
        //                _position++;
        //                return Token.Colon(_position);
        //            }

        //            if (symbol == '"')
        //            {
        //                _subState = SubState.String;
        //                return null;
        //            }

        //            if (symbol.IsEnd)
        //                return Token.Empty(_position);

        //            if (UnknownDelimiters.Contains(symbol))
        //            {
        //                return null;
        //            }

        //            _identifierStartPos = _position;
        //            _subState = SubState.Identifier;
        //            return null;

        //        case SubState.Identifier:
        //            if (symbol.IsEnd || delimiters.Contains(symbol))
        //            {
        //                var identifier = Token.Identifier(SubStr(_identifierStartPos, _position - 1), _identifierStartPos, _position - 1);

        //                _subState = SubState.Initial;
        //                return identifier;
        //            }

        //            return null;

        //        case SubState.String:
        //            if (symbol == '"')
        //            {
        //                _subState = SubState.Initial;
        //                return null;
        //            }

        //            if (symbol.IsEnd)
        //            {
        //                throw new NotSupportedException("string constant in property path hasn't end quotes");
        //            }
        //            return null;

        //        default:
        //            throw new NotSupportedException(string.Format("Identifier lexer: unexpected state '{0}'", _subState));
        //    }

        //}

        //#endregion


        #region Help methods

        private string SubStr(string str, int start, int end)
        {
            return str.Substring(start, end - start + 1);
        }

        /// <summary>
        /// Found out whether xaml namespace:class is enum class or not. If yes, return enum type, otherwise - null 
        /// </summary>
        /// <param name="namespace"></param>
        /// <param name="class"></param>
        /// <returns></returns>
        private Type TakeEnum(string fullTypeName)
        {
            var @type = _typeResolver.Resolve(fullTypeName);

            if (@type != null && @type.IsEnum)
                return @type;
            return null;
        } 

        #endregion


        #region Nested types

        class Chunk
        {
        //    public TokenType TokenType { get; private set; }

            public string Value { get; private set; }

        //    public static Token Empty(int position)
        //    {
        //        return new Token(TokenType.Empty, null, position, position);
        //    }

        //    public static Token Dot(int position)
        //    {
        //        return new Token(TokenType.Dot, ".", position, position);
        //    }

        //    public static Token Colon(int position)
        //    {
        //        return new Token(TokenType.Colon, ":", position, position);
        //    }

        //    public static Token Identifier(string identifier, int startPosition, int endPosition)
        //    {
        //        return new Token(TokenType.Identifier, identifier, startPosition, endPosition);
        //    }

        //    public bool IsIdentifier
        //    {
        //        get
        //        {
        //            return TokenType == PropertyPathAnalyzer.TokenType.Identifier;
        //        }
        //    }

        //    public bool IsDot
        //    {
        //        get
        //        {
        //            return TokenType == PropertyPathAnalyzer.TokenType.Dot;
        //        }
        //    }

        //    public bool IsColon
        //    {
        //        get
        //        {
        //            return TokenType == PropertyPathAnalyzer.TokenType.Colon;
        //        }
        //    }

        //    public bool IsEmpty
        //    {
        //        get
        //        {
        //            return TokenType == PropertyPathAnalyzer.TokenType.Empty;
        //        }
        //    }

            public int Start { get; private set; }
            public int End { get; private set; }

            public Chunk(string value, int startPosition, int endPosition)
            {
                //TokenType = type;
                Value = value;
                Start = startPosition;
                End = endPosition;
            }
        }

        //enum State
        //{
        //    Initial,
        //    MathClass,
        //    Identifier,
        //    StaticPropPathDot,
        //    PropPathDot
        //}

        //enum SubState
        //{
        //    Initial,
        //    Identifier,
        //    String
        //}

        //enum TokenType
        //{
        //    Identifier,
        //    Dot,
        //    Colon,
        //    Empty
        //}

        //class Token
        //{
        //    public TokenType TokenType { get; private set; }

        //    public string Value { get; private set; }

        //    public static Token Empty(int position)
        //    {
        //        return new Token(TokenType.Empty, null, position, position);
        //    }

        //    public static Token Dot(int position)
        //    {
        //        return new Token(TokenType.Dot, ".", position, position);
        //    }

        //    public static Token Colon(int position)
        //    {
        //        return new Token(TokenType.Colon, ":", position, position);
        //    }

        //    public static Token Identifier(string identifier, int startPosition, int endPosition)
        //    {
        //        return new Token(TokenType.Identifier, identifier, startPosition, endPosition);
        //    }

        //    public bool IsIdentifier
        //    {
        //        get
        //        {
        //            return TokenType == PropertyPathAnalyzer.TokenType.Identifier;
        //        }
        //    }

        //    public bool IsDot
        //    {
        //        get
        //        {
        //            return TokenType == PropertyPathAnalyzer.TokenType.Dot;
        //        }
        //    }

        //    public bool IsColon
        //    {
        //        get
        //        {
        //            return TokenType == PropertyPathAnalyzer.TokenType.Colon;
        //        }
        //    }

        //    public bool IsEmpty
        //    {
        //        get
        //        {
        //            return TokenType == PropertyPathAnalyzer.TokenType.Empty;
        //        }
        //    }

        //    public int Start { get; private set; }
        //    public int End { get; private set; }

        //    private Token(TokenType type, string value, int startPosition, int endPosition)
        //    {
        //        TokenType = type;
        //        Value = value;
        //        Start = startPosition;
        //        End = endPosition;
        //    }
        //}

        //class Symbol
        //{
        //    private char _c;
        //    public bool IsEnd { get; private set; }
        //    public static readonly Symbol End = new Symbol() { IsEnd = true };

        //    protected Symbol()
        //    {

        //    }

        //    public Symbol(char c)
        //    {
        //        _c = c;
        //        IsEnd = false;
        //    }

        //    public static implicit operator Symbol(Char c)
        //    {
        //        return new Symbol(c);
        //    }

        //    public static implicit operator Char(Symbol symbol)
        //    {
        //        if (symbol.IsEnd)
        //            throw new NotSupportedException("Symbol to char: End symbol couldn't be translated to char");

        //        return symbol._c;
        //    }

        //    public static implicit operator String(Symbol symbol)
        //    {
        //        return new String(new[] { (Char)symbol });
        //    }

        //    public static bool operator ==(Symbol symbol, Char c)
        //    {
        //        if (symbol.IsEnd)
        //            return false;

        //        return symbol._c == c;
        //    }

        //    public static bool operator !=(Symbol symbol, Char c)
        //    {
        //        if (symbol.IsEnd)
        //            return true;

        //        return symbol._c != c;
        //    }
        //}

        #endregion
    }
}
