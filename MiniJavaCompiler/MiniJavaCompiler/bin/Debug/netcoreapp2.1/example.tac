proc firstclass
_AX = 0
endp firstclass

proc secondclass
wrs S0
rdi _bp-2
_bp-4 = 10
_bp-8 = 20
_bp-10 = _bp-2 * _bp-4
_bp-12 = _bp-8 + _bp-10
_bp-6 = _bp-12
wrs S1
wri _bp-6
wrln
_AX = _bp-6
endp secondclass

proc main
call secondclass
endp main
