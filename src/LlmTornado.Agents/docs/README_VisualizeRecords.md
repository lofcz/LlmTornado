# Runner Record Visualization - Sample Outputs

This folder contains sample outputs demonstrating the RunnerRecord visualization capabilities.

## Sample Files

### 1. DOT Graph Output (`sample_orchestration.dot`)
- **Format**: Graphviz DOT format
- **Purpose**: Creates flow diagrams showing state transitions with resource usage metrics
- **Features**: 
  - Color-coded nodes (green=low usage, yellow=medium, red=high)
  - Token usage and execution time displayed
  - Transition arrows with step numbers
  - Ready for Graphviz rendering

**How to View:**
```bash
# Using Graphviz command line
dot -Tpng sample_orchestration.dot -o sample_orchestration.png

# Online viewer
# Visit: http://magjac.com/graphviz-visual-editor/
# Copy/paste the .dot file content
```

### 2. PlantUML Output (`sample_orchestration.puml`)
- **Format**: PlantUML state diagram
- **Purpose**: Creates UML-style state diagrams with metrics
- **Features**:
  - State boxes with internal metrics
  - Clear start/end points
  - Transition labels
  - Professional UML formatting

**How to View:**
```bash
# Using PlantUML command line
java -jar plantuml.jar sample_orchestration.puml

# Online viewer
# Visit: http://www.plantuml.com/plantuml/
# Copy/paste the .puml file content
```

### 3. Summary Report (`sample_orchestration_summary.txt`)
- **Format**: Plain text report
- **Purpose**: Detailed metrics and analysis
- **Features**:
  - Total execution statistics
  - Per-state breakdown
  - Average metrics
  - Resource usage ranking

## Example Usage Code (`RunnerRecordVisualizationExample.cs`)
Complete working example showing:
- How to set up orchestration with step recording
- How to generate visualizations
- How to save files for sharing
- Integration patterns for real orchestrations

## Integration Steps

1. **Enable Recording**: Set `orchestration.RecordSteps = true` before running
2. **Run Orchestration**: Execute your orchestration workflow
3. **Generate Visuals**: Use extension methods on `orchestration.RunSteps`
4. **Save Files**: Use async file save methods for sharing/documentation

## Color Coding in DOT Graphs

- **Green**: Low resource usage (< 500 tokens OR < 2 seconds)
- **Yellow**: Medium resource usage (500-1000 tokens OR 2-5 seconds)  
- **Red**: High resource usage (> 1000 tokens OR > 5 seconds)

## Use Cases

- **Debugging**: Identify bottlenecks and problematic states
- **Performance Analysis**: Track resource consumption patterns
- **Documentation**: Generate visual documentation of workflows
- **Monitoring**: Create dashboards showing execution patterns
- **Optimization**: Find opportunities for performance improvements

## Output Formats Supported

| Format | Extension | Best For | Rendering Tools |
|--------|-----------|----------|-----------------|
| DOT Graph | `.dot` | Flow diagrams | Graphviz, VS Code extensions |
| PlantUML | `.puml` | UML diagrams | PlantUML, online viewers |
| Summary | `.txt` | Reports | Any text editor |
