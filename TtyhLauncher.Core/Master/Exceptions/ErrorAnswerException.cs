using System;

namespace TtyhLauncher.Master.Exceptions {
    public class ErrorAnswerException : Exception {
        public ErrorAnswerException(string msg) : base(msg) {
        }
    }
}