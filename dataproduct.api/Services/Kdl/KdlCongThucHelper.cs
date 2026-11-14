using System.Collections.Concurrent;
using System.Globalization;

namespace dataproduct.api.Services.Kdl
{
    /// <summary>
    /// Bộ tính công thức của KDL_Key.CongThuc: [MaKey], số, + - * /, ngoặc, dấu âm.
    /// Quy ước giá trị: không có dữ liệu → tính như 0 (giống ô trống Excel); null (không xác định) → kết quả null;
    /// mọi toán hạng đều không có dữ liệu → kết quả không có dữ liệu (không lưu). Chia cho 0 → null.
    /// Công thức đã parse được cache theo chuỗi.
    /// </summary>
    public class KdlCongThucHelper
    {
        private readonly ConcurrentDictionary<string, Node> _cache = new();

        /// <summary>Giá trị của 1 key: CoDuLieu = false nghĩa là không có dữ liệu.</summary>
        public readonly record struct GiaTri(bool CoDuLieu, decimal? So)
        {
            public static readonly GiaTri Rong = new(false, null);
        }

        public IReadOnlyCollection<string> LayKeyThamChieu(string congThuc)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ThuThapKey(Parse(congThuc), keys);
            return keys;
        }

        /// <summary>Kiểm tra cú pháp; trả thông báo lỗi hoặc null.</summary>
        public string? KiemTra(string congThuc)
        {
            try { Parse(congThuc); return null; }
            catch (FormatException ex) { return ex.Message; }
        }

        public GiaTri Tinh(string congThuc, Func<string, GiaTri> layGiaTri)
            => Eval(Parse(congThuc), layGiaTri);

        // ── AST ────────────────────────────────────────────────────────────────

        private abstract record Node;
        private sealed record SoNode(decimal GiaTri) : Node;
        private sealed record KeyNode(string MaKey) : Node;
        private sealed record AmNode(Node Con) : Node;
        private sealed record PhepNode(char Phep, Node Trai, Node Phai) : Node;

        private Node Parse(string congThuc) => _cache.GetOrAdd(congThuc.Trim(), s => new Parser(s).ParseToanBo());

        private static void ThuThapKey(Node n, HashSet<string> keys)
        {
            switch (n)
            {
                case KeyNode k: keys.Add(k.MaKey); break;
                case AmNode a: ThuThapKey(a.Con, keys); break;
                case PhepNode p: ThuThapKey(p.Trai, keys); ThuThapKey(p.Phai, keys); break;
            }
        }

        private static GiaTri Eval(Node n, Func<string, GiaTri> lay)
        {
            switch (n)
            {
                case SoNode s:
                    return new GiaTri(true, s.GiaTri);
                case KeyNode k:
                    return lay(k.MaKey);
                case AmNode a:
                    var c = Eval(a.Con, lay);
                    return c.CoDuLieu ? new GiaTri(true, -c.So) : c;
                case PhepNode p:
                    var t = Eval(p.Trai, lay);
                    var f = Eval(p.Phai, lay);
                    if (!t.CoDuLieu && !f.CoDuLieu) return GiaTri.Rong;
                    if ((t.CoDuLieu && t.So == null) || (f.CoDuLieu && f.So == null)) return new GiaTri(true, null);
                    var a1 = t.So ?? 0m;
                    var b1 = f.So ?? 0m;
                    return p.Phep switch
                    {
                        '+' => new GiaTri(true, a1 + b1),
                        '-' => new GiaTri(true, a1 - b1),
                        '*' => new GiaTri(true, a1 * b1),
                        '/' => new GiaTri(true, b1 == 0 ? null : a1 / b1),
                        _ => throw new InvalidOperationException($"Phép toán không hỗ trợ: {p.Phep}"),
                    };
                default:
                    throw new InvalidOperationException("Nút công thức không hợp lệ.");
            }
        }

        // ── Parser (đệ quy xuống) ──────────────────────────────────────────────

        private sealed class Parser(string s)
        {
            private int _i;

            public Node ParseToanBo()
            {
                if (string.IsNullOrWhiteSpace(s)) throw new FormatException("Công thức rỗng.");
                var n = ParseCong();
                BoTrang();
                if (_i < s.Length) throw new FormatException($"Ký tự thừa '{s[_i]}' tại vị trí {_i + 1} trong công thức \"{s}\".");
                return n;
            }

            private Node ParseCong()
            {
                var n = ParseNhan();
                while (true)
                {
                    BoTrang();
                    if (_i < s.Length && (s[_i] == '+' || s[_i] == '-'))
                    {
                        var op = s[_i++];
                        n = new PhepNode(op, n, ParseNhan());
                    }
                    else return n;
                }
            }

            private Node ParseNhan()
            {
                var n = ParseDon();
                while (true)
                {
                    BoTrang();
                    if (_i < s.Length && (s[_i] == '*' || s[_i] == '/'))
                    {
                        var op = s[_i++];
                        n = new PhepNode(op, n, ParseDon());
                    }
                    else return n;
                }
            }

            private Node ParseDon()
            {
                BoTrang();
                if (_i >= s.Length) throw new FormatException($"Công thức \"{s}\" kết thúc đột ngột.");

                var ch = s[_i];
                if (ch == '-') { _i++; return new AmNode(ParseDon()); }
                if (ch == '+') { _i++; return ParseDon(); }
                if (ch == '(')
                {
                    _i++;
                    var n = ParseCong();
                    BoTrang();
                    if (_i >= s.Length || s[_i] != ')') throw new FormatException($"Thiếu ')' trong công thức \"{s}\".");
                    _i++;
                    return n;
                }
                if (ch == '[')
                {
                    var dong = s.IndexOf(']', _i);
                    if (dong < 0) throw new FormatException($"Thiếu ']' trong công thức \"{s}\".");
                    var key = s.Substring(_i + 1, dong - _i - 1).Trim();
                    if (key.Length == 0) throw new FormatException($"Tên key rỗng trong công thức \"{s}\".");
                    _i = dong + 1;
                    return new KeyNode(key);
                }
                if (char.IsDigit(ch) || ch == '.')
                {
                    var bd = _i;
                    while (_i < s.Length && (char.IsDigit(s[_i]) || s[_i] == '.')) _i++;
                    if (!decimal.TryParse(s.AsSpan(bd, _i - bd), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var so))
                        throw new FormatException($"Số không hợp lệ '{s[bd.._i]}' trong công thức \"{s}\".");
                    return new SoNode(so);
                }
                throw new FormatException($"Ký tự không hợp lệ '{ch}' tại vị trí {_i + 1} trong công thức \"{s}\".");
            }

            private void BoTrang()
            {
                while (_i < s.Length && char.IsWhiteSpace(s[_i])) _i++;
            }
        }
    }
}
