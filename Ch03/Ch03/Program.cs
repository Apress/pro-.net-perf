using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ch03
{
    public class CompanyPolicy
    {
        public bool CanTakeVacation(Employee e) { return true; }
    }

    public class Employee
    {
        private int id;
        private string name;
        private static CompanyPolicy policy;

        public virtual void Work()
        {
            Console.WriteLine("Zzzz...");
        }
        public void TakeVacation(int days)
        {
            if (policy.CanTakeVacation(this))
                Console.WriteLine("Zzzz...");
        }
        public static void SetCompanyPolicy(CompanyPolicy newPolicy)
        {
            policy = newPolicy;
        }
    }

    public sealed class Manager : Employee
    {
        public override void Work()
        {
            Console.WriteLine("--sound of rustling papers--");
        }
    }

    public class MegaBase
    {
        public virtual void F1() { }
        public virtual void F2() { }
        public virtual void F3() { }
        public virtual void F4() { }
        public virtual void F5() { }
        public virtual void F6() { }
        public virtual void F7() { }
        public virtual void F8() { }
        public virtual void G1() { }
        public virtual void G2() { }
        public virtual void G3() { }
        public virtual void G4() { }
        public virtual void G5() { }
        public virtual void G6() { }
        public virtual void G7() { }
        public virtual void G8() { }
    }
    public class SmallDerived : MegaBase, IComparable
    {
        public override void G4()
        {
        }

        public int CompareTo(object obj)
        {
            return 0;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Employee e = new Manager();
            e.Work();
            Employee.SetCompanyPolicy(new CompanyPolicy());
            e.TakeVacation(1);

            SmallDerived sd = new SmallDerived();
            sd.F1();
            sd.F2();
            sd.F3();
            sd.F4();
            sd.F5();
            sd.F6();
            sd.F7();
            sd.F8();
            sd.G1();
            sd.G2();
            sd.G3();
            sd.G4();
            sd.G5();
            sd.G6();
            sd.G7();
            sd.G8();
            
            Console.ReadLine();
        }
    }
}
