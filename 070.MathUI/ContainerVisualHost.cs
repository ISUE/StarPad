using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace starPadSDK.WPFHelp {
    // Create a host visual derived from the FrameworkElement class.
    // This class provides layout, event handling, and container support for
    // the child visual objects.
    // Add children to ContainerVisual in reverse z-order (bottom to top).
    public class ContainerVisualHost : FrameworkElement {
        private ContainerVisual _containerVisual;

        public VisualCollection Children { get { return _containerVisual.Children; } }

        public ContainerVisualHost() {
            // Create a ContainerVisual to hold DrawingVisual children.
            _containerVisual = new ContainerVisual();

            // Create parent-child relationship with host visual and ContainerVisual.
            this.AddVisualChild(_containerVisual);
        }

        // Provide a required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount {
            get { return _containerVisual == null ? 0 : 1; }
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index) {
            if(_containerVisual == null) {
                throw new ArgumentOutOfRangeException();
            }

            return _containerVisual;
        }
    }
}
