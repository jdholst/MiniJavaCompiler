class factorial { // comment
  /* multi
    line */
  /* multi but single */
  public static /*in between */ void main (String[] a) {
    System.out.println(new Fac().ComputeFac(10));
  }
}
class Fac{
  public int ComputeFac(int num) {
    int num_aux;
    if (num < 1.0) num_aux=1;
    else num_aux= num*(this.ComputeFac(num-1));
    return num_aux;
  }
}