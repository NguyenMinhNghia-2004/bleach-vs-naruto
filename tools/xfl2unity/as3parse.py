"""Parser for the ActionScript 3 subset used in BVN frame scripts.

Produces a compact JSON-friendly AST (nested lists) that the Unity Script VM
(Assets/BVN/Runtime/Script) interprets. Type annotations are parsed and dropped.

Statements                              Expressions
  ["expr", e]                             ["num", n] ["str", s] ["bool", b] ["null"] ["undef"] ["this"]
  ["var", [[name, e|None], ...]]          ["id", name]
  ["fn", name, [[p, default|None]], body] ["get", obj, name]       obj.name
  ["if", cond, then, else|None]           ["idx", obj, key]        obj[key]
  ["ret", e|None]                         ["call", callee, [args]]
  ["block", [stmts]]                      ["new", callee, [args]]
  ["switch", e, [[case|None, [stmts]]]]   ["arr", [items]] ["obj", [[key, e], ...]]
  ["for", init, cond, update, body]       ["un", op, e]  ["pre", op, target]  ["post", op, target]
  ["forin", name, obj, body, each]        ["bin", op, a, b]  ["and", a, b]  ["or", a, b]
  ["while", cond, body] ["do", body, cond] ["cond", c, a, b]
  ["break"] ["continue"]                  ["set", op, target, value]   (op '=' '+=' ...)
                                          ["func", [[p, default]], body]  (function expression)
"""
import re

KEYWORDS = {
    'var', 'const', 'function', 'if', 'else', 'return', 'switch', 'case', 'default', 'for', 'while',
    'do', 'break', 'continue', 'new', 'true', 'false', 'null', 'undefined', 'this', 'typeof',
    'in', 'is', 'as', 'instanceof', 'each', 'delete', 'void', 'import',
}

TOKEN_RE = re.compile(r'''
    (?P<ws>[ \t\r\n ﻿　]+)
  | (?P<lcomment>//[^\n]*)
  | (?P<bcomment>/\*.*?\*/)
  | (?P<num>0[xX][0-9a-fA-F]+|(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?)
  | (?P<str>"(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*')
  | (?P<name>[A-Za-z_$\u0080-￿][\w$\u0080-￿]*)
  | (?P<op>>>>=|===|!==|>>>|<<=|>>=|\+\+|--|&&|\|\||==|!=|<=|>=|\+=|-=|\*=|/=|%=|&=|\|=|\^=|<<|>>|<>|::|\.\.\.|[{}()\[\];,.:?+\-*/%!<>=&|^~@])
''', re.S | re.X)

ESCAPES = {'n': '\n', 't': '\t', 'r': '\r', 'b': '\b', 'f': '\f', 'v': '\v', '0': '\0'}


class ParseError(Exception):
    pass


class Tok:
    __slots__ = ('kind', 'val', 'nl', 'pos')

    def __init__(self, kind, val, nl, pos):
        self.kind, self.val, self.nl, self.pos = kind, val, nl, pos

    def __repr__(self):
        return '%s:%r' % (self.kind, self.val)


def _unescape(s):
    out, i = [], 0
    while i < len(s):
        ch = s[i]
        if ch == '\\' and i + 1 < len(s):
            nx = s[i + 1]
            if nx == 'u' and i + 5 < len(s) + 0:
                try:
                    out.append(chr(int(s[i + 2:i + 6], 16)))
                    i += 6
                    continue
                except ValueError:
                    pass
            if nx == 'x':
                try:
                    out.append(chr(int(s[i + 2:i + 4], 16)))
                    i += 4
                    continue
                except ValueError:
                    pass
            out.append(ESCAPES.get(nx, nx))
            i += 2
        else:
            out.append(ch)
            i += 1
    return ''.join(out)


def tokenize(src):
    toks, pos, nl = [], 0, False
    while pos < len(src):
        m = TOKEN_RE.match(src, pos)
        if not m:
            raise ParseError('bad character %r at %d' % (src[pos], pos))
        kind = m.lastgroup
        text = m.group(kind)
        if kind in ('ws', 'lcomment', 'bcomment'):
            if '\n' in text or kind == 'lcomment':
                nl = True
        elif kind == 'num':
            v = int(text, 16) if text[:2] in ('0x', '0X') else float(text)
            if isinstance(v, float) and v.is_integer() and 'e' not in text.lower() and '.' not in text:
                v = int(v)
            toks.append(Tok('num', v, nl, pos))
            nl = False
        elif kind == 'str':
            toks.append(Tok('str', _unescape(text[1:-1]), nl, pos))
            nl = False
        elif kind == 'name':
            toks.append(Tok('kw' if text in KEYWORDS else 'name', text, nl, pos))
            nl = False
        else:
            toks.append(Tok('op', '!=' if text == '<>' else text, nl, pos))
            nl = False
        pos = m.end()
    toks.append(Tok('eof', None, True, pos))
    return toks


BINARY_PREC = [
    ('||',), ('&&',), ('|',), ('^',), ('&',),
    ('==', '!=', '===', '!=='),
    ('<', '>', '<=', '>=', 'is', 'as', 'in', 'instanceof'),
    ('<<', '>>', '>>>'),
    ('+', '-'),
    ('*', '/', '%'),
]
ASSIGN_OPS = {'=', '+=', '-=', '*=', '/=', '%=', '&=', '|=', '^=', '<<=', '>>=', '>>>='}


class Parser:
    def __init__(self, src):
        self.t = tokenize(src)
        self.i = 0

    # -- helpers
    @property
    def cur(self):
        return self.t[self.i]

    def peek(self, k=1):
        return self.t[min(self.i + k, len(self.t) - 1)]

    def at(self, val, kind=None):
        c = self.cur
        return c.val == val and (kind is None or c.kind == kind) and c.kind in ('op', 'kw', 'name')

    def eat(self, val):
        if self.at(val):
            self.i += 1
            return True
        return False

    def expect(self, val):
        if not self.eat(val):
            raise ParseError('expected %r got %r at %d' % (val, self.cur.val, self.cur.pos))

    def name(self):
        c = self.cur
        if c.kind in ('name', 'kw'):
            self.i += 1
            return c.val
        raise ParseError('expected name got %r at %d' % (c.val, c.pos))

    def end_stmt(self):
        if self.eat(';'):
            return
        c = self.cur
        if c.kind == 'eof' or c.val == '}' or c.nl:
            return
        raise ParseError('expected ; got %r at %d' % (c.val, c.pos))

    def skip_type(self):
        if self.eat(':'):
            if self.eat('*'):
                return
            if self.at('*='):  # 'var x:*=value' lexes as '*='
                self.t[self.i] = Tok('op', '=', False, self.cur.pos)
                return
            self.name()
            while self.at('.') and self.peek().kind in ('name', 'kw'):
                self.i += 1
                self.name()
            if self.at('.') and self.peek().val == '<':  # Vector.<T>
                self.i += 2
                self.skip_type_name()
                self.expect('>')

    def skip_type_name(self):
        if self.eat('*'):
            return
        self.name()
        while self.eat('.'):
            if self.at('<'):
                self.i += 1
                self.skip_type_name()
                self.expect('>')
                return
            self.name()

    # -- statements
    def program(self):
        body = []
        while self.cur.kind != 'eof':
            st = self.statement()
            if st is not None:
                body.append(st)
        return body

    def block(self):
        self.expect('{')
        body = []
        while not self.at('}'):
            if self.cur.kind == 'eof':
                raise ParseError('unexpected eof in block')
            st = self.statement()
            if st is not None:
                body.append(st)
        self.expect('}')
        return ['block', body]

    def statement(self):
        c = self.cur
        if c.kind == 'op':
            if c.val == ';':
                self.i += 1
                return None
            if c.val == '{':
                return self.block()
        if c.kind == 'kw':
            v = c.val
            if v == 'import':
                while not self.at(';') and not self.cur.nl or self.cur is c:
                    self.i += 1
                    if self.cur.kind == 'eof':
                        break
                self.eat(';')
                return None
            if v in ('var', 'const'):
                self.i += 1
                st = self.var_decls()
                self.end_stmt()
                return st
            if v == 'function' and self.peek().kind in ('name', 'kw'):
                self.i += 1
                name = self.name()
                if name in ('get', 'set') and self.cur.kind in ('name', 'kw') and not self.at('('):
                    name = self.name()
                params, body = self.func_rest()
                return ['fn', name, params, body]
            if v == 'if':
                self.i += 1
                self.expect('(')
                cond = self.expression()
                self.expect(')')
                then = self.statement()
                other = None
                if self.eat('else'):
                    other = self.statement()
                return ['if', cond, then, other]
            if v == 'return':
                self.i += 1
                val = None
                if not (self.at(';') or self.at('}') or self.cur.nl or self.cur.kind == 'eof'):
                    val = self.expression()
                self.end_stmt()
                return ['ret', val]
            if v in ('break', 'continue'):
                self.i += 1
                if self.cur.kind == 'name' and not self.cur.nl:
                    self.i += 1  # labels ignored
                self.end_stmt()
                return [v]
            if v == 'switch':
                return self.switch()
            if v == 'while':
                self.i += 1
                self.expect('(')
                cond = self.expression()
                self.expect(')')
                return ['while', cond, self.statement()]
            if v == 'do':
                self.i += 1
                body = self.statement()
                self.expect('while')
                self.expect('(')
                cond = self.expression()
                self.expect(')')
                self.end_stmt()
                return ['do', body, cond]
            if v == 'for':
                return self.for_stmt()
        e = self.expression()
        self.end_stmt()
        return ['expr', e]

    def var_decls(self):
        decls = []
        while True:
            name = self.name()
            self.skip_type()
            init = None
            if self.eat('='):
                init = self.assign()
            decls.append([name, init])
            if not self.eat(','):
                break
        return ['var', decls]

    def func_rest(self):
        self.expect('(')
        params = []
        while not self.at(')'):
            if self.eat('...'):
                params.append(['...' + self.name(), None])
                self.skip_type()
            else:
                p = self.name()
                self.skip_type()
                d = None
                if self.eat('='):
                    d = self.assign()
                params.append([p, d])
            if not self.eat(','):
                break
        self.expect(')')
        self.skip_type()
        body = self.block()
        return params, body

    def switch(self):
        self.i += 1
        self.expect('(')
        disc = self.expression()
        self.expect(')')
        self.expect('{')
        cases = []
        while not self.eat('}'):
            if self.eat('case'):
                test = self.expression()
            else:
                self.expect('default')
                test = None
            self.expect(':')
            body = []
            while not (self.at('case') or self.at('default') or self.at('}')):
                st = self.statement()
                if st is not None:
                    body.append(st)
            cases.append([test, body])
        return ['switch', disc, cases]

    def for_stmt(self):
        self.i += 1
        each = self.eat('each')
        self.expect('(')
        init = None
        if self.at('var') or self.at('const'):
            save = self.i
            self.i += 1
            name = self.name()
            self.skip_type()
            if self.eat('in'):
                obj = self.expression()
                self.expect(')')
                return ['forin', name, obj, self.statement(), each]
            self.i = save + 1
            init = self.var_decls()
        elif not self.at(';'):
            save = self.i
            if self.cur.kind == 'name' and self.peek().val == 'in':
                name = self.name()
                self.expect('in')
                obj = self.expression()
                self.expect(')')
                return ['forin', name, obj, self.statement(), each]
            self.i = save
            init = ['expr', self.expression()]
        self.expect(';')
        cond = None if self.at(';') else self.expression()
        self.expect(';')
        update = None if self.at(')') else self.expression()
        self.expect(')')
        return ['for', init, cond, update, self.statement()]

    # -- expressions
    def expression(self):
        e = self.assign()
        if self.at(','):
            seq = [e]
            while self.eat(','):
                seq.append(self.assign())
            return ['seq', seq]
        return e

    def assign(self):
        left = self.conditional()
        c = self.cur
        if c.kind == 'op' and c.val in ASSIGN_OPS:
            if left[0] not in ('id', 'get', 'idx'):
                raise ParseError('bad assignment target at %d' % c.pos)
            self.i += 1
            return ['set', c.val, left, self.assign()]
        return left

    def conditional(self):
        c = self.binary(0)
        if self.eat('?'):
            a = self.assign()
            self.expect(':')
            b = self.assign()
            return ['cond', c, a, b]
        return c

    def binary(self, level):
        if level >= len(BINARY_PREC):
            return self.unary()
        ops = BINARY_PREC[level]
        left = self.binary(level + 1)
        while self.cur.kind in ('op', 'kw') and self.cur.val in ops:
            op = self.cur.val
            self.i += 1
            right = self.binary(level + 1)
            if op == '&&':
                left = ['and', left, right]
            elif op == '||':
                left = ['or', left, right]
            else:
                left = ['bin', op, left, right]
        return left

    def unary(self):
        c = self.cur
        if c.kind == 'op' and c.val in ('!', '-', '+', '~'):
            self.i += 1
            return ['un', c.val, self.unary()]
        if c.kind == 'op' and c.val in ('++', '--'):
            self.i += 1
            return ['pre', c.val, self.unary()]
        if c.kind == 'kw' and c.val in ('typeof', 'delete', 'void'):
            self.i += 1
            return ['un', c.val, self.unary()]
        e = self.postfix()
        c = self.cur
        if c.kind == 'op' and c.val in ('++', '--') and not c.nl:
            self.i += 1
            return ['post', c.val, e]
        return e

    def postfix(self):
        if self.at('new'):
            self.i += 1
            callee = self.member_only(self.primary())
            args = self.args() if self.at('(') else []
            e = ['new', callee, args]
        else:
            e = self.primary()
        while True:
            if self.at('.'):
                self.i += 1
                if self.at('<'):  # Vector.<T>
                    self.i += 1
                    self.skip_type_name()
                    self.expect('>')
                    continue
                e = ['get', e, self.name()]
            elif self.at('['):
                self.i += 1
                k = self.expression()
                self.expect(']')
                e = ['idx', e, k]
            elif self.at('('):
                e = ['call', e, self.args()]
            else:
                return e

    def member_only(self, e):
        while True:
            if self.at('.'):
                self.i += 1
                if self.at('<'):
                    self.i += 1
                    self.skip_type_name()
                    self.expect('>')
                    continue
                e = ['get', e, self.name()]
            elif self.at('['):
                self.i += 1
                k = self.expression()
                self.expect(']')
                e = ['idx', e, k]
            else:
                return e

    def args(self):
        self.expect('(')
        out = []
        while not self.at(')'):
            out.append(self.assign())
            if not self.eat(','):
                break
        self.expect(')')
        return out

    def primary(self):
        c = self.cur
        if c.kind == 'num':
            self.i += 1
            return ['num', c.val]
        if c.kind == 'str':
            self.i += 1
            return ['str', c.val]
        if c.kind == 'name':
            self.i += 1
            return ['id', c.val]
        if c.kind == 'kw':
            v = c.val
            if v in ('true', 'false'):
                self.i += 1
                return ['bool', v == 'true']
            if v == 'null':
                self.i += 1
                return ['null']
            if v == 'undefined':
                self.i += 1
                return ['undef']
            if v == 'this':
                self.i += 1
                return ['this']
            if v == 'function':
                self.i += 1
                if self.cur.kind in ('name', 'kw') and not self.at('('):
                    self.name()
                params, body = self.func_rest()
                return ['func', params, body]
            if v in ('each', 'get', 'set', 'is', 'as', 'in'):
                self.i += 1
                return ['id', v]
        if c.kind == 'op':
            if c.val == '(':
                self.i += 1
                e = self.expression()
                self.expect(')')
                return e
            if c.val == '[':
                self.i += 1
                items = []
                while not self.at(']'):
                    items.append(self.assign())
                    if not self.eat(','):
                        break
                self.expect(']')
                return ['arr', items]
            if c.val == '{':
                self.i += 1
                props = []
                while not self.at('}'):
                    k = self.cur
                    if k.kind in ('name', 'kw', 'str'):
                        key = k.val
                    elif k.kind == 'num':
                        key = str(k.val)
                    else:
                        raise ParseError('bad object key %r at %d' % (k.val, k.pos))
                    self.i += 1
                    self.expect(':')
                    props.append([key, self.assign()])
                    if not self.eat(','):
                        break
                self.expect('}')
                return ['obj', props]
        raise ParseError('unexpected %r at %d' % (c.val, c.pos))


def parse(src):
    return Parser(src).program()


if __name__ == '__main__':
    import json
    import sys
    print(json.dumps(parse(open(sys.argv[1], encoding='utf-8').read()), ensure_ascii=False, indent=1))
