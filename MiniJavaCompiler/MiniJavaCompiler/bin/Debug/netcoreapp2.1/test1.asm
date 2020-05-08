.model small
.stack 100h
.data
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
mov ax, [bp-2]
mov bx, [bp-4]
imul bx
mov [bp-12], ax
mov ax, [bp-12]
mov [bp-6], ax
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
