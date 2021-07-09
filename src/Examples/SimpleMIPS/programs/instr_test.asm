#Make sure the $sp has enough wiggle room
subi $sp, $sp, 4
j sll

# Stores whether $a0 == $a1 onto mem[$sp--]
push_result:
sw $ra, 0($sp)
jal is_equal
lw $ra, 0($sp)
sw $v0, 0($sp)
subi $sp, $sp, 4
jr $ra

# Computes $v0 = $a0 == $a1
is_equal:
beq $a0, $a1, is_equal_true
ori $v0, $zero, 0
j is_equal_return
is_equal_true:
ori $v0, $zero, 1
is_equal_return:
jr $ra

sll:
ori $t0, $zero, 1
sll $a0, $t0, 1
ori $a1, $zero, 2
jal push_result

srl:
ori $t0, $zero, 2
srl $a0, $t0, 1
ori $a1, $zero, 1
jal push_result

add:
ori $t0, $zero, 4
subi $t1, $zero, 2 # -2
add $a0, $t0, $t1
ori $a1, $zero, 2
jal push_result

addu:
ori $t0, $zero, 4
ori $t1, $zero, 2
addu $a0, $t0, $t1
ori $a1, $zero, 6
jal push_result

sub:
ori $t0, $zero, 4
ori $t1, $zero, 2
sub $a0, $t0, $t1
ori $a1, $zero, 2
jal push_result

subu:
ori $t0, $zero, 4
ori $t1, $zero, 2
subu $a0, $t0, $t1
ori $a1, $zero, 2
jal push_result

and:
ori $t0, $zero, 12
ori $t1, $zero, 10
and $a0, $t0, $t1
ori $a1, $zero, 8
jal push_result

or:
ori $t0, $zero, 12
ori $t1, $zero, 10
or $a0, $t0, $t1
ori $a1, $zero, 14
jal push_result

slt0:
subi $t0, $zero, 5 # -5
ori $t1, $zero, 10
slt $a0, $t0, $t1
ori $a1, $zero, 1
jal push_result

slt1:
subi $t1, $zero, 5 # -5
ori $t0, $zero, 10
slt $a0, $t0, $t1
ori $a1, $zero, 0
jal push_result

sltu0:
subi $t0, $zero, 5 # -5
ori $t1, $zero, 10
sltu $a0, $t0, $t1
ori $a1, $zero, 0
jal push_result

sltu1:
subi $t1, $zero, 5 # -5
ori $t0, $zero, 10
sltu $a0, $t0, $t1
ori $a1, $zero, 1
jal push_result

bne:
ori $t0, $zero, 10
ori $t1, $zero, 5
bne $t0, $t1, bne_should_hit
ori $a0, $zero, 0
j bne_post
bne_should_hit:
ori $a0, $zero, 1
bne_post:
ori $a1, $zero, 1
jal push_result

addi:
ori $t0, $zero, 4
addi $a0, $t0, -2
ori $a1, $zero, 2
jal push_result

andi:
ori $t0, $zero, 12
andi $a0, $t0, 10
ori $a1, $zero, 8
jal push_result

#terminate