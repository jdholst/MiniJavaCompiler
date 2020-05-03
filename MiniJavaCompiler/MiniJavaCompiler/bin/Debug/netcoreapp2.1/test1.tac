proc firstclass
endp firstclass

proc secondclass
_bp-8 = 5
_bp-2 = _bp-8
_bp-10 = 10
_bp-4 = _bp-10
_bp-12 = _bp-2 * _bp-4
_bp-6 = _bp-12
endp secondclass

proc main
call secondclass
endp main
