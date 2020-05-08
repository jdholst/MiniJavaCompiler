proc firstclass
endp firstclass

proc secondclass
_bp-8 = 5
_bp-2 = _bp-8
_bp-10 = 10
_bp-4 = _bp-10
_bp-12 = _bp-4 - _bp-2
_bp-6 = _bp-12
wrs S0
wri _bp-6
wrln
_bp-14 = _bp-4 / _bp-2
_bp-6 = _bp-14
wrs S1
wri _bp-6
wrln
_AX = _bp-6
endp secondclass

proc main
call secondclass
endp main
