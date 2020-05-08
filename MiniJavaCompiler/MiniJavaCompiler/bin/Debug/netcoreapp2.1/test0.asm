.model small
.stack 100h
.data
S0 DB "Subtract: ", "$"
S1 DB "Divide: ", "$"
.code
include io.asm
start PROC
mov ax, @data
mov ds, ax
call main
mov ah, 04ch
int 21h
start ENDP

firstclass PROC
push bp
mov bp, sp
sub sp, 0
add sp, 0
pop bp
ret 0
firstclass ENDP

secondclass PROC
push bp
mov bp, sp
sub sp, 6
mov ax, 5
mov [bp-8], ax
mov ax, [bp-8]
mov [bp-2], ax
mov ax, 10
mov [bp-10], ax
mov ax, [bp-10]
mov [bp-4], ax
mov ax, [bp-4]
sub ax, [bp-2]
mov [bp-12], ax
mov ax, [bp-12]
mov [bp-6], ax
mov dx, offset s0
call writestr
mov dx, [bp-6]
call writeint
call writeln
mov ax, [bp-4]
cwd 
mov bx, [bp-2]
idiv bx
mov [bp-14], ax
mov ax, [bp-14]
mov [bp-6], ax
mov dx, offset s1
call writestr
mov dx, [bp-6]
call writeint
call writeln
mov ax, [bp-6]
add sp, 6
pop bp
ret 0
secondclass ENDP

main PROC
call secondclass
ret 
main ENDP

END start
