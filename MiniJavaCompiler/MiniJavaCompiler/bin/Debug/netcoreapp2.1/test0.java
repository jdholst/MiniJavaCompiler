class firstclass{
  public void firstclass(){
    return;
  }
  public int secondclass(){
    int a,b,c;

    a=5;
    b=10;
    c=b-a;
    write("Subtract: ");
    writeln(c);
    c=b/a;
    write("Divide: ");
    writeln(c);
    return c;
  }
}
final class Main{
  public static void main(String [] args){
    firstclass.secondclass();
  }
}
