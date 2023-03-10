using UnityEngine;
using static GraphViewBase.ShortcutHandler;

namespace GraphViewBase {
    public class KeyAction {
        public class Handler {
            public SpecialKey[] checks;
            public Actions actionType;

            public Handler(SpecialKey[] checks, Actions actionType) {
                this.checks = checks;
                this.actionType = actionType;
            }
        }

        private KeyCode keyCode;
        public readonly Handler handler = null;
        public readonly Handler altHandler = null;
        private string displayName = null;

        public string DisplayName => displayName;

        public KeyAction(KeyCode keyCode, Handler handler, Handler altHandler = null) {
            this.handler = handler;
            this.altHandler = altHandler;
            this.keyCode= keyCode;
            CreateDisplayName();
        }

        private void CreateDisplayName() {
            void CreateDisplayCodeFromHandler(Handler handler, ref string displayCode) {
                foreach (SpecialKey specialKey in handler.checks) {
                    if (specialKeyDisplayNameLookup.ContainsKey(specialKey)) {
                        displayCode += specialKeyDisplayNameLookup[specialKey] + "+";
                    }
                }
            }
            string displayCode = "";
            CreateDisplayCodeFromHandler(handler, ref displayCode);
            if (altHandler != null) {
                displayCode += "/ ";
                CreateDisplayCodeFromHandler(altHandler, ref displayCode);
            }
            displayName = $"{handler.actionType} ({displayCode + keyCode})";
        }

        private Actions ExecuteBase(EventModifiers modifiers, Handler handler, Handler alternativeHandler = null) {
            if (AreChecksValid(modifiers, handler.checks)) {
                return handler.actionType;
            } else if (alternativeHandler != null && AreChecksValid(modifiers, alternativeHandler.checks)) {
                return alternativeHandler.actionType;
            }
            return Actions.NoAction;
        }

        public Actions Execute(EventModifiers modifiers) {
            return ExecuteBase(modifiers, handler, altHandler);
        }

        private bool AreChecksValid(EventModifiers eventModifiers, SpecialKey[] checks) {
            for (int i = 0; i < checks.Length; i++) {
                if (!specialKeyChecks[checks[i]](eventModifiers)) {
                    return false;
                }
            }
            return true;
        }
    }
}
