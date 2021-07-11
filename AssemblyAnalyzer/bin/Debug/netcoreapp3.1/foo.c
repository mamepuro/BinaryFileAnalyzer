#include <stdio.h>

void bar(int c)
{
    printf("bar is %d", c);
}

int foo(int a, int b)
{
    int c = a + b;
    bar(c);
    return c;
}

int main()
{
    int a = foo(1, 2);
    printf("foo is %d", a);
}