using System;
using System.Collections.Generic;
using System.Globalization;
using BVN.Data;
using BVN.Flash;

namespace BVN.Script
{
    // ===================================================================== scopes & control flow
    public sealed class Scope
    {
        public readonly Dictionary<string, object> Vars;
        public readonly Scope Parent;
        public readonly object This;
        public readonly MovieClip Clip;       // timeline owner (root scope)

        public Scope(Dictionary<string, object> vars, Scope parent, object thisObj, MovieClip clip)
        {
            Vars = vars;
            Parent = parent;
            This = thisObj;
            Clip = clip;
        }
    }

    enum Flow : byte { Normal, Return, Break, Continue }

    sealed class Ctx
    {
        public Flow Flow;
        public object Ret;
    }

    // ===================================================================== AST nodes
    abstract class Expr
    {
        public abstract object Eval(Scope s);
    }

    abstract class Stmt
    {
        public abstract void Exec(Scope s, Ctx c);
    }

    sealed class Lit : Expr
    {
        readonly object _v;
        public Lit(object v) { _v = v; }
        public override object Eval(Scope s) => _v;
    }

    sealed class ThisExpr : Expr
    {
        public override object Eval(Scope s) => s.This;
    }

    sealed class Ident : Expr
    {
        public readonly string Name;
        public Ident(string n) { Name = n; }
        public override object Eval(Scope s) => Runtime.Lookup(s, Name);
    }

    sealed class Member : Expr
    {
        public readonly Expr Obj;
        public readonly string Name;
        public Member(Expr o, string n) { Obj = o; Name = n; }
        public override object Eval(Scope s) => Runtime.GetMember(Obj.Eval(s), Name);
    }

    sealed class Index : Expr
    {
        public readonly Expr Obj, Key;
        public Index(Expr o, Expr k) { Obj = o; Key = k; }
        public override object Eval(Scope s)
        {
            var o = Obj.Eval(s);
            var k = Key.Eval(s);
            if (o is List<object> l && Ops.IsNumeric(k))
            {
                int i = Ops.ToInt(k);
                return i >= 0 && i < l.Count ? l[i] : Undefined.Value;
            }
            return Runtime.GetMember(o, Ops.ToStr(k));
        }
    }

    sealed class Call : Expr
    {
        readonly Expr _callee;
        readonly Expr[] _args;
        public Call(Expr callee, Expr[] args) { _callee = callee; _args = args; }

        public override object Eval(Scope s)
        {
            var args = new object[_args.Length];
            switch (_callee)
            {
                case Member m:
                {
                    var o = m.Obj.Eval(s);
                    for (int i = 0; i < args.Length; i++) args[i] = _args[i].Eval(s);
                    return Runtime.CallMethod(o, m.Name, args);
                }
                case Index ix:
                {
                    var o = ix.Obj.Eval(s);
                    var k = Ops.ToStr(ix.Key.Eval(s));
                    for (int i = 0; i < args.Length; i++) args[i] = _args[i].Eval(s);
                    return Runtime.CallMethod(o, k, args);
                }
                case Ident id:
                {
                    for (int i = 0; i < args.Length; i++) args[i] = _args[i].Eval(s);
                    return Runtime.CallIdent(s, id.Name, args);
                }
                default:
                {
                    var f = _callee.Eval(s);
                    for (int i = 0; i < args.Length; i++) args[i] = _args[i].Eval(s);
                    return Runtime.Invoke(f, s.This, args, "(expression)");
                }
            }
        }
    }

    sealed class NewExpr : Expr
    {
        readonly Expr _callee;
        readonly Expr[] _args;
        public NewExpr(Expr callee, Expr[] args) { _callee = callee; _args = args; }

        public override object Eval(Scope s)
        {
            var args = new object[_args.Length];
            for (int i = 0; i < args.Length; i++) args[i] = _args[i].Eval(s);
            string name = _callee is Ident id ? id.Name : _callee is Member m ? m.Name : null;
            var cls = name != null && _callee is Ident ? Runtime.Lookup(s, name) : _callee.Eval(s);
            return Runtime.Construct(cls, name, args, s);
        }
    }

    sealed class ArrLit : Expr
    {
        readonly Expr[] _items;
        public ArrLit(Expr[] items) { _items = items; }
        public override object Eval(Scope s)
        {
            var l = new List<object>(_items.Length);
            foreach (var e in _items) l.Add(e.Eval(s));
            return l;
        }
    }

    sealed class ObjLit : Expr
    {
        readonly string[] _keys;
        readonly Expr[] _vals;
        public ObjLit(string[] k, Expr[] v) { _keys = k; _vals = v; }
        public override object Eval(Scope s)
        {
            var o = new ASObject();
            for (int i = 0; i < _keys.Length; i++) o[_keys[i]] = _vals[i].Eval(s);
            return o;
        }
    }

    sealed class FuncExpr : Expr
    {
        readonly FuncDef _def;
        public FuncExpr(FuncDef d) { _def = d; }
        public override object Eval(Scope s) => new ScriptFunction(_def, s);
    }

    sealed class Unary : Expr
    {
        readonly string _op;
        readonly Expr _e;
        public Unary(string op, Expr e) { _op = op; _e = e; }
        public override object Eval(Scope s)
        {
            switch (_op)
            {
                case "!": return !Ops.ToBoolean(_e.Eval(s));
                case "-": return -Ops.ToNumber(_e.Eval(s));
                case "+": return Ops.ToNumber(_e.Eval(s));
                case "~": return (double)~Ops.ToInt(_e.Eval(s));
                case "typeof": return Runtime.TypeOf(_e.Eval(s));
                case "void": _e.Eval(s); return Undefined.Value;
                case "delete":
                    if (_e is Member m && m.Obj.Eval(s) is ASObject ao) { ao.Remove(m.Name); return true; }
                    if (_e is Index ix && ix.Obj.Eval(s) is ASObject ao2) { ao2.Remove(Ops.ToStr(ix.Key.Eval(s))); return true; }
                    return false;
            }
            throw new ScriptException("bad unary " + _op);
        }
    }

    sealed class Logic : Expr
    {
        readonly bool _and;
        readonly Expr _a, _b;
        public Logic(bool and, Expr a, Expr b) { _and = and; _a = a; _b = b; }
        public override object Eval(Scope s)
        {
            var a = _a.Eval(s);
            if (_and) return Ops.ToBoolean(a) ? _b.Eval(s) : a;
            return Ops.ToBoolean(a) ? a : _b.Eval(s);
        }
    }

    sealed class Cond : Expr
    {
        readonly Expr _c, _a, _b;
        public Cond(Expr c, Expr a, Expr b) { _c = c; _a = a; _b = b; }
        public override object Eval(Scope s) => Ops.ToBoolean(_c.Eval(s)) ? _a.Eval(s) : _b.Eval(s);
    }

    sealed class Seq : Expr
    {
        readonly Expr[] _es;
        public Seq(Expr[] es) { _es = es; }
        public override object Eval(Scope s)
        {
            object v = Undefined.Value;
            foreach (var e in _es) v = e.Eval(s);
            return v;
        }
    }

    sealed class Binary : Expr
    {
        readonly string _op;
        readonly Expr _a, _b;
        public Binary(string op, Expr a, Expr b) { _op = op; _a = a; _b = b; }
        public override object Eval(Scope s) => Apply(_op, _a.Eval(s), _b.Eval(s), s);

        public static object Apply(string op, object a, object b, Scope s)
        {
            switch (op)
            {
                case "+": return Ops.Add(a, b);
                case "-": return Ops.ToNumber(a) - Ops.ToNumber(b);
                case "*": return Ops.ToNumber(a) * Ops.ToNumber(b);
                case "/": return Ops.ToNumber(a) / Ops.ToNumber(b);
                case "%": return Ops.ToNumber(a) % Ops.ToNumber(b);
                case "==": return Ops.LooseEquals(a, b);
                case "!=": return !Ops.LooseEquals(a, b);
                case "===": return Ops.StrictEquals(a, b);
                case "!==": return !Ops.StrictEquals(a, b);
                case "<": { int c = Ops.Compare(a, b, out var u); return !u && c < 0; }
                case ">": { int c = Ops.Compare(a, b, out var u); return !u && c > 0; }
                case "<=": { int c = Ops.Compare(a, b, out var u); return !u && c <= 0; }
                case ">=": { int c = Ops.Compare(a, b, out var u); return !u && c >= 0; }
                case "&": return (double)(Ops.ToInt(a) & Ops.ToInt(b));
                case "|": return (double)(Ops.ToInt(a) | Ops.ToInt(b));
                case "^": return (double)(Ops.ToInt(a) ^ Ops.ToInt(b));
                case "<<": return (double)(Ops.ToInt(a) << (Ops.ToInt(b) & 31));
                case ">>": return (double)(Ops.ToInt(a) >> (Ops.ToInt(b) & 31));
                case ">>>": return (double)(Ops.ToUint(a) >> (Ops.ToInt(b) & 31));
                case "is": return Runtime.IsType(a, b);
                case "instanceof": return Runtime.IsType(a, b);
                case "as": return Ops.ToBoolean(Runtime.IsType(a, b)) ? a : null;
                case "in":
                    if (b is ASObject ao) return ao.ContainsKey(Ops.ToStr(a));
                    if (b is List<object> l) { int i = Ops.ToInt(a); return i >= 0 && i < l.Count; }
                    return !(Runtime.GetMember(b, Ops.ToStr(a)) is Undefined);
            }
            throw new ScriptException("bad operator " + op);
        }
    }

    sealed class Assign : Expr
    {
        readonly string _op;
        readonly Expr _target, _value;
        public Assign(string op, Expr t, Expr v) { _op = op; _target = t; _value = v; }

        public override object Eval(Scope s)
        {
            switch (_target)
            {
                case Ident id:
                {
                    object v = _value.Eval(s);
                    if (_op != "=") v = Binary.Apply(_op.Substring(0, _op.Length - 1), Runtime.Lookup(s, id.Name), v, s);
                    Runtime.Store(s, id.Name, v);
                    return v;
                }
                case Member m:
                {
                    var o = m.Obj.Eval(s);
                    object v = _value.Eval(s);
                    if (_op != "=") v = Binary.Apply(_op.Substring(0, _op.Length - 1), Runtime.GetMember(o, m.Name), v, s);
                    Runtime.SetMember(o, m.Name, v);
                    return v;
                }
                case Index ix:
                {
                    var o = ix.Obj.Eval(s);
                    var k = ix.Key.Eval(s);
                    object v = _value.Eval(s);
                    if (o is List<object> l && Ops.IsNumeric(k))
                    {
                        int i = Ops.ToInt(k);
                        if (_op != "=") v = Binary.Apply(_op.Substring(0, _op.Length - 1), i < l.Count ? l[i] : Undefined.Value, v, s);
                        while (l.Count <= i) l.Add(Undefined.Value);
                        l[i] = v;
                        return v;
                    }
                    string key = Ops.ToStr(k);
                    if (_op != "=") v = Binary.Apply(_op.Substring(0, _op.Length - 1), Runtime.GetMember(o, key), v, s);
                    Runtime.SetMember(o, key, v);
                    return v;
                }
            }
            throw new ScriptException("bad assignment target");
        }
    }

    sealed class IncDec : Expr
    {
        readonly bool _prefix;
        readonly double _delta;
        readonly Expr _target;
        public IncDec(bool prefix, string op, Expr t) { _prefix = prefix; _delta = op == "++" ? 1 : -1; _target = t; }

        public override object Eval(Scope s)
        {
            double old;
            switch (_target)
            {
                case Ident id:
                    old = Ops.ToNumber(Runtime.Lookup(s, id.Name));
                    Runtime.Store(s, id.Name, old + _delta);
                    break;
                case Member m:
                {
                    var o = m.Obj.Eval(s);
                    old = Ops.ToNumber(Runtime.GetMember(o, m.Name));
                    Runtime.SetMember(o, m.Name, old + _delta);
                    break;
                }
                case Index ix:
                {
                    var o = ix.Obj.Eval(s);
                    var k = ix.Key.Eval(s);
                    if (o is List<object> l && Ops.IsNumeric(k))
                    {
                        int i = Ops.ToInt(k);
                        old = i < l.Count ? Ops.ToNumber(l[i]) : double.NaN;
                        while (l.Count <= i) l.Add(Undefined.Value);
                        l[i] = old + _delta;
                    }
                    else
                    {
                        old = Ops.ToNumber(Runtime.GetMember(o, Ops.ToStr(k)));
                        Runtime.SetMember(o, Ops.ToStr(k), old + _delta);
                    }
                    break;
                }
                default: throw new ScriptException("bad ++/-- target");
            }
            return _prefix ? old + _delta : old;
        }
    }

    // ----------------------------------------------------------------- statements
    sealed class ExprStmt : Stmt
    {
        readonly Expr _e;
        public ExprStmt(Expr e) { _e = e; }
        public override void Exec(Scope s, Ctx c) => _e.Eval(s);
    }

    sealed class VarStmt : Stmt
    {
        readonly string[] _names;
        readonly Expr[] _inits;
        public VarStmt(string[] n, Expr[] i) { _names = n; _inits = i; }
        public override void Exec(Scope s, Ctx c)
        {
            for (int i = 0; i < _names.Length; i++)
            {
                if (_inits[i] == null)
                {
                    if (!s.Vars.ContainsKey(_names[i])) s.Vars[_names[i]] = Undefined.Value;
                    continue;
                }
                s.Vars[_names[i]] = _inits[i].Eval(s);
            }
        }
    }

    sealed class FnStmt : Stmt
    {
        public readonly FuncDef Def;
        public FnStmt(FuncDef d) { Def = d; }
        public override void Exec(Scope s, Ctx c) => s.Vars[Def.Name] = new ScriptFunction(Def, s);
    }

    sealed class IfStmt : Stmt
    {
        readonly Expr _c;
        readonly Stmt _a, _b;
        public IfStmt(Expr c, Stmt a, Stmt b) { _c = c; _a = a; _b = b; }
        public override void Exec(Scope s, Ctx c)
        {
            if (Ops.ToBoolean(_c.Eval(s))) _a?.Exec(s, c);
            else _b?.Exec(s, c);
        }
    }

    sealed class Block : Stmt
    {
        public readonly Stmt[] Body;
        public Block(Stmt[] b) { Body = b; }
        public override void Exec(Scope s, Ctx c)
        {
            foreach (var st in Body)
            {
                st.Exec(s, c);
                if (c.Flow != Flow.Normal) return;
            }
        }
    }

    sealed class ReturnStmt : Stmt
    {
        readonly Expr _e;
        public ReturnStmt(Expr e) { _e = e; }
        public override void Exec(Scope s, Ctx c)
        {
            c.Ret = _e != null ? _e.Eval(s) : Undefined.Value;
            c.Flow = Flow.Return;
        }
    }

    sealed class JumpStmt : Stmt
    {
        readonly Flow _f;
        public JumpStmt(Flow f) { _f = f; }
        public override void Exec(Scope s, Ctx c) => c.Flow = _f;
    }

    sealed class WhileStmt : Stmt
    {
        readonly Expr _cond;
        readonly Stmt _body;
        readonly bool _doWhile;
        public WhileStmt(Expr cond, Stmt body, bool doWhile) { _cond = cond; _body = body; _doWhile = doWhile; }
        public override void Exec(Scope s, Ctx c)
        {
            int guard = 0;
            bool first = _doWhile;
            while (first || Ops.ToBoolean(_cond.Eval(s)))
            {
                first = false;
                if (++guard > 1000000) throw new ScriptException("loop limit");
                _body?.Exec(s, c);
                if (c.Flow == Flow.Break) { c.Flow = Flow.Normal; return; }
                if (c.Flow == Flow.Continue) c.Flow = Flow.Normal;
                if (c.Flow == Flow.Return) return;
            }
        }
    }

    sealed class ForStmt : Stmt
    {
        readonly Stmt _init;
        readonly Expr _cond, _update;
        readonly Stmt _body;
        public ForStmt(Stmt init, Expr cond, Expr update, Stmt body) { _init = init; _cond = cond; _update = update; _body = body; }
        public override void Exec(Scope s, Ctx c)
        {
            _init?.Exec(s, c);
            int guard = 0;
            while (_cond == null || Ops.ToBoolean(_cond.Eval(s)))
            {
                if (++guard > 1000000) throw new ScriptException("loop limit");
                _body?.Exec(s, c);
                if (c.Flow == Flow.Break) { c.Flow = Flow.Normal; return; }
                if (c.Flow == Flow.Continue) c.Flow = Flow.Normal;
                if (c.Flow == Flow.Return) return;
                _update?.Eval(s);
            }
        }
    }

    sealed class ForInStmt : Stmt
    {
        readonly string _var;
        readonly Expr _obj;
        readonly Stmt _body;
        readonly bool _each;
        public ForInStmt(string v, Expr o, Stmt b, bool each) { _var = v; _obj = o; _body = b; _each = each; }
        public override void Exec(Scope s, Ctx c)
        {
            var o = _obj.Eval(s);
            var items = new List<object>();
            switch (o)
            {
                case List<object> l:
                    for (int i = 0; i < l.Count; i++) items.Add(_each ? l[i] : (object)Ops.NumStr(i));
                    break;
                case ASObject d:
                    foreach (var kv in d) items.Add(_each ? kv.Value : kv.Key);
                    break;
            }
            foreach (var it in items)
            {
                Runtime.Store(s, _var, it, true);
                _body?.Exec(s, c);
                if (c.Flow == Flow.Break) { c.Flow = Flow.Normal; return; }
                if (c.Flow == Flow.Continue) c.Flow = Flow.Normal;
                if (c.Flow == Flow.Return) return;
            }
        }
    }

    sealed class SwitchStmt : Stmt
    {
        readonly Expr _disc;
        readonly Expr[] _tests;
        readonly Stmt[][] _bodies;
        public SwitchStmt(Expr d, Expr[] t, Stmt[][] b) { _disc = d; _tests = t; _bodies = b; }
        public override void Exec(Scope s, Ctx c)
        {
            var v = _disc.Eval(s);
            int start = -1;
            for (int i = 0; i < _tests.Length && start < 0; i++)
                if (_tests[i] != null && Ops.StrictEquals(v, _tests[i].Eval(s))) start = i;
            if (start < 0)
                for (int i = 0; i < _tests.Length; i++)
                    if (_tests[i] == null) { start = i; break; }
            if (start < 0) return;
            for (int i = start; i < _bodies.Length; i++)
                foreach (var st in _bodies[i])
                {
                    st.Exec(s, c);
                    if (c.Flow == Flow.Break) { c.Flow = Flow.Normal; return; }
                    if (c.Flow != Flow.Normal) return;
                }
        }
    }

    // ===================================================================== functions
    sealed class FuncDef
    {
        public string Name;
        public string[] Params;
        public Expr[] Defaults;
        public string Rest;
        public Block Body;
        public string[] HoistedVars;
        public FnStmt[] HoistedFns;
    }

    public sealed class ScriptFunction : IScriptCallable
    {
        readonly FuncDef _def;
        readonly Scope _closure;
        internal ScriptFunction(FuncDef def, Scope closure) { _def = def; _closure = closure; }
        public string Name => _def.Name;

        public object Call(object thisObj, object[] args)
        {
            var vars = new Dictionary<string, object>();
            var self = _closure.Parent == null && _closure.Clip != null ? _closure.Clip : thisObj ?? _closure.This;
            var scope = new Scope(vars, _closure, self, _closure.Clip);
            for (int i = 0; i < _def.Params.Length; i++)
            {
                object v = i < args.Length ? args[i] : _def.Defaults[i] != null ? _def.Defaults[i].Eval(scope) : Undefined.Value;
                vars[_def.Params[i]] = v;
            }
            if (_def.Rest != null)
            {
                var rest = new List<object>();
                for (int i = _def.Params.Length; i < args.Length; i++) rest.Add(args[i]);
                vars[_def.Rest] = rest;
            }
            vars["arguments"] = new List<object>(args);
            foreach (var v in _def.HoistedVars) if (!vars.ContainsKey(v)) vars[v] = Undefined.Value;
            foreach (var f in _def.HoistedFns) f.Exec(scope, null);
            var ctx = new Ctx();
            _def.Body.Exec(scope, ctx);
            return ctx.Flow == Flow.Return ? ctx.Ret : Undefined.Value;
        }

        public override string ToString() => "[function " + _def.Name + "]";
    }

    /// <summary>Compiled frame script.</summary>
    public sealed class CompiledScript
    {
        internal Stmt[] Body;
        internal string[] TopVars;
        internal FuncDef[] TopFns;
    }

    // ===================================================================== AST → nodes
    static class Compiler
    {
        public static CompiledScript Compile(object ast)
        {
            var list = ast as List<object>;
            var cs = new CompiledScript();
            if (list == null) { cs.Body = Array.Empty<Stmt>(); cs.TopVars = Array.Empty<string>(); cs.TopFns = Array.Empty<FuncDef>(); return cs; }
            var vars = new List<string>();
            var fns = new List<FuncDef>();
            var body = new List<Stmt>();
            foreach (var st in list)
            {
                var s = Stmt(st);
                if (s == null) continue;
                body.Add(s);
            }
            CollectDecls(list, vars, fns, true);
            cs.Body = body.ToArray();
            cs.TopVars = vars.ToArray();
            cs.TopFns = fns.ToArray();
            return cs;
        }

        static void CollectDecls(List<object> stmts, List<string> vars, List<FuncDef> fns, bool top)
        {
            foreach (var o in stmts)
            {
                var a = o as List<object>;
                if (a == null || a.Count == 0) continue;
                switch ((string)a[0])
                {
                    case "var":
                        foreach (var d in (List<object>)a[1]) vars.Add((string)((List<object>)d)[0]);
                        break;
                    case "fn":
                        fns.Add(Func(a));
                        break;
                    case "block": CollectDecls((List<object>)a[1], vars, fns, top); break;
                    case "if":
                        if (a[2] is List<object> t) CollectDecls(new List<object> { t }, vars, fns, top);
                        if (a[3] is List<object> e) CollectDecls(new List<object> { e }, vars, fns, top);
                        break;
                    case "for": if (a[1] is List<object> init) CollectDecls(new List<object> { init }, vars, fns, top); if (a[4] is List<object> fb) CollectDecls(new List<object> { fb }, vars, fns, top); break;
                    case "forin": vars.Add((string)a[1]); if (a[3] is List<object> b2) CollectDecls(new List<object> { b2 }, vars, fns, top); break;
                    case "while": if (a[2] is List<object> wb) CollectDecls(new List<object> { wb }, vars, fns, top); break;
                    case "do": if (a[1] is List<object> db) CollectDecls(new List<object> { db }, vars, fns, top); break;
                    case "switch":
                        foreach (var cse in (List<object>)a[2]) CollectDecls((List<object>)((List<object>)cse)[1], vars, fns, top);
                        break;
                }
            }
        }

        static FuncDef Func(List<object> a)
        {
            // ["fn", name, params, body] or ["func", params, body]
            bool named = (string)a[0] == "fn";
            var ps = (List<object>)a[named ? 2 : 1];
            var body = (List<object>)a[named ? 3 : 2];
            var def = new FuncDef { Name = named ? (string)a[1] : "anonymous" };
            var names = new List<string>();
            var defs = new List<Expr>();
            foreach (var p in ps)
            {
                var pa = (List<object>)p;
                var pn = (string)pa[0];
                if (pn.StartsWith("...")) { def.Rest = pn.Substring(3); continue; }
                names.Add(pn);
                defs.Add(pa[1] == null ? null : Expr(pa[1]));
            }
            def.Params = names.ToArray();
            def.Defaults = defs.ToArray();
            var bodyStmts = (List<object>)body[1];
            var vars = new List<string>();
            var fns = new List<FuncDef>();
            CollectDecls(bodyStmts, vars, fns, false);
            def.HoistedVars = vars.ToArray();
            def.HoistedFns = fns.ConvertAll(f => new FnStmt(f)).ToArray();
            var stmts = new List<Stmt>();
            foreach (var st in bodyStmts)
            {
                var s = Stmt(st, true);
                if (s != null) stmts.Add(s);
            }
            def.Body = new Block(stmts.ToArray());
            return def;
        }

        static Stmt Stmt(object o, bool inFunction = false)
        {
            var a = o as List<object>;
            if (a == null) return null;
            switch ((string)a[0])
            {
                case "expr": return new ExprStmt(Expr(a[1]));
                case "var":
                {
                    var ds = (List<object>)a[1];
                    var names = new string[ds.Count];
                    var inits = new Expr[ds.Count];
                    for (int i = 0; i < ds.Count; i++)
                    {
                        var d = (List<object>)ds[i];
                        names[i] = (string)d[0];
                        inits[i] = d[1] == null ? null : Expr(d[1]);
                    }
                    return new VarStmt(names, inits);
                }
                case "fn": return inFunction ? null : new FnStmt(Func(a)); // nested fns are hoisted
                case "if": return new IfStmt(Expr(a[1]), Stmt(a[2], inFunction), a[3] == null ? null : Stmt(a[3], inFunction));
                case "ret": return new ReturnStmt(a[1] == null ? null : Expr(a[1]));
                case "block":
                {
                    var l = new List<Stmt>();
                    foreach (var st in (List<object>)a[1])
                    {
                        var s = Stmt(st, inFunction);
                        if (s != null) l.Add(s);
                    }
                    return new Block(l.ToArray());
                }
                case "break": return new JumpStmt(Flow.Break);
                case "continue": return new JumpStmt(Flow.Continue);
                case "while": return new WhileStmt(Expr(a[1]), Stmt(a[2], inFunction), false);
                case "do": return new WhileStmt(Expr(a[2]), Stmt(a[1], inFunction), true);
                case "for": return new ForStmt(a[1] == null ? null : Stmt(a[1], inFunction), a[2] == null ? null : Expr(a[2]), a[3] == null ? null : Expr(a[3]), Stmt(a[4], inFunction));
                case "forin": return new ForInStmt((string)a[1], Expr(a[2]), Stmt(a[3], inFunction), a.Count > 4 && a[4] is bool b && b);
                case "switch":
                {
                    var cases = (List<object>)a[2];
                    var tests = new Expr[cases.Count];
                    var bodies = new Stmt[cases.Count][];
                    for (int i = 0; i < cases.Count; i++)
                    {
                        var cse = (List<object>)cases[i];
                        tests[i] = cse[0] == null ? null : Expr(cse[0]);
                        var l = new List<Stmt>();
                        foreach (var st in (List<object>)cse[1])
                        {
                            var s = Stmt(st, inFunction);
                            if (s != null) l.Add(s);
                        }
                        bodies[i] = l.ToArray();
                    }
                    return new SwitchStmt(Expr(a[1]), tests, bodies);
                }
            }
            throw new ScriptException("unknown statement " + a[0]);
        }

        static Expr[] Exprs(object o)
        {
            var l = (List<object>)o;
            var r = new Expr[l.Count];
            for (int i = 0; i < r.Length; i++) r[i] = Expr(l[i]);
            return r;
        }

        static Expr Expr(object o)
        {
            var a = (List<object>)o;
            switch ((string)a[0])
            {
                case "num": return new Lit(J.Num(a[1]));
                case "str": return new Lit((string)a[1]);
                case "bool": return new Lit((bool)a[1]);
                case "null": return new Lit(null);
                case "undef": return new Lit(Undefined.Value);
                case "this": return new ThisExpr();
                case "id": return new Ident((string)a[1]);
                case "get": return new Member(Expr(a[1]), (string)a[2]);
                case "idx": return new Index(Expr(a[1]), Expr(a[2]));
                case "call": return new Call(Expr(a[1]), Exprs(a[2]));
                case "new": return new NewExpr(Expr(a[1]), Exprs(a[2]));
                case "arr": return new ArrLit(Exprs(a[1]));
                case "obj":
                {
                    var props = (List<object>)a[1];
                    var keys = new string[props.Count];
                    var vals = new Expr[props.Count];
                    for (int i = 0; i < props.Count; i++)
                    {
                        var p = (List<object>)props[i];
                        keys[i] = (string)p[0];
                        vals[i] = Expr(p[1]);
                    }
                    return new ObjLit(keys, vals);
                }
                case "un": return new Unary((string)a[1], Expr(a[2]));
                case "pre": return new IncDec(true, (string)a[1], Expr(a[2]));
                case "post": return new IncDec(false, (string)a[1], Expr(a[2]));
                case "bin": return new Binary((string)a[1], Expr(a[2]), Expr(a[3]));
                case "and": return new Logic(true, Expr(a[1]), Expr(a[2]));
                case "or": return new Logic(false, Expr(a[1]), Expr(a[2]));
                case "cond": return new Cond(Expr(a[1]), Expr(a[2]), Expr(a[3]));
                case "set": return new Assign((string)a[1], Expr(a[2]), Expr(a[3]));
                case "seq": return new Seq(Exprs(a[1]));
                case "func": return new FuncExpr(Func(a));
            }
            throw new ScriptException("unknown expression " + a[0]);
        }
    }
}
