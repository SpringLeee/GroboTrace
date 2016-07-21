using System;
using System.Reflection;
using System.Text;

using GroboTrace.Mono.Cecil.Metadata;
using GroboTrace.Mono.Collections.Generic;

namespace GroboTrace.Mono.Cecil.Cil
{
    public sealed class MethodBody
    {
        public MethodBody()
        {
            Instructions = new InstructionCollection();
            ExceptionHandlers = new Collection<ExceptionHandler>();
            ilCodeBaker = new ILCodeBaker(Instructions);
            exceptionsBaker = new ExceptionsBaker(ExceptionHandlers, Instructions);

            
            InitLocals = true;
        }

        public int MaxStackSize { get; set; }

        public void RecalculateMaxStackSize(Module module)
        {
            MaxStackSize = new MaxStackSizeCalculator(this, module).TryComputeMaxStack();
        }

        public bool InitLocals { get; set; }

        public MetadataToken LocalVarToken { get; set; }

        public bool HasExceptionHandlers { get { return !ExceptionHandlers.IsNullOrEmpty(); } }

        public bool HasVariables { get { return variablesCount > 0; } }


        // todo: использовать SignatureHelper 
        public byte[] VariablesSignature { get { return variablesSignature ?? (variablesSignature = new byte[0]); } set { variablesSignature = value; } }

        public byte[] BakeILCode()
        {
            return ilCodeBaker.BakeILCode();
        }

        public byte[] BakeExceptions()
        {
            return exceptionsBaker.BakeExceptions();
        }

        public byte[] BakeFullMethodBody(Module module, Func<byte[], MetadataToken> signatureTokenBuilder, int? maxStack)
        {
            return new MethodBodyBaker(module,signatureTokenBuilder, this, maxStack).BakeMethodBody();
        }


        public ILProcessor GetILProcessor()
        {
            return new ILProcessor(this);
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            result.AppendLine("Instructions:");
            foreach (var instruction in Instructions)
            {
                result.AppendLine(instruction.ToString());
            }

            result.AppendLine();

            result.AppendLine("Exception handlers:");
            foreach (var exceptionHandler in ExceptionHandlers)
            {
                result.AppendLine(exceptionHandler.ToString());
            }

            return result.ToString();
        }

        //internal int max_stack_size;
        //internal int code_size;
        //internal bool init_locals;
        //internal MetadataToken local_var_token;

        public bool isTiny;

        private readonly ILCodeBaker ilCodeBaker;
        private readonly ExceptionsBaker exceptionsBaker;

        public readonly Collection<Instruction> Instructions;
        public readonly Collection<ExceptionHandler> ExceptionHandlers;
        private byte[] variablesSignature;
        internal uint variablesCount;
    }

    internal class InstructionCollection : Collection<Instruction>
    {
        internal InstructionCollection()
        {
        }

        internal InstructionCollection(int capacity)
            : base(capacity)
        {
        }

        protected override void OnAdd(Instruction item, int index)
        {
            if(index == 0)
                return;

            var previous = items[index - 1];
            previous.Next = item;
            item.Previous = previous;
        }

        protected override void OnInsert(Instruction item, int index)
        {
            if(size == 0)
                return;

            var current = items[index];
            if(current == null)
            {
                var last = items[index - 1];
                last.Next = item;
                item.Previous = last;
                return;
            }

            var previous = current.Previous;
            if(previous != null)
            {
                previous.Next = item;
                item.Previous = previous;
            }

            current.Previous = item;
            item.Next = current;
        }

        protected override void OnSet(Instruction item, int index)
        {
            var current = items[index];

            item.Previous = current.Previous;
            item.Next = current.Next;

            current.Previous = null;
            current.Next = null;
        }

        protected override void OnRemove(Instruction item, int index)
        {
            var previous = item.Previous;
            if(previous != null)
                previous.Next = item.Next;

            var next = item.Next;
            if(next != null)
                next.Previous = item.Previous;

            item.Previous = null;
            item.Next = null;
        }
    }
}