proc fun
_bp-6 = _bp+4 * _bp+6
_bp+8 = _bp-6
endp fun

proc testit
_bp-8 = 5
_bp-2 = _bp-8
_bp-10 = 10
_bp-4 = _bp-10
push _bp-6
push _bp-4
push _bp-2
call fun
endp testit

proc main
endp main
