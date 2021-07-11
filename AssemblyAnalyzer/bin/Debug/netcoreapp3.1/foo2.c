#include <stdio.h>

int bar(int a, int b)
{
    int c = a + b;
    printf("x\n");
    return c;
}
int foo(int a, int b)
{
    int c = bar(a,b);
    return c;
}

int main()
{
    int a = foo(1,2);
    printf("%d\n", a);
}