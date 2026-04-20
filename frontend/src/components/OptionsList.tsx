interface OptionsListProps {
  options: string[];
  onChange: (options: string[]) => void;
}

export default function OptionsList({ options, onChange }: OptionsListProps) {
  const updateOption = (index: number, value: string) => {
    const updated = [...options];
    updated[index] = value;
    onChange(updated);
  };

  const addOption = () => {
    onChange([...options, '']);
  };

  const removeOption = (index: number) => {
    if (options.length <= 2) return;
    onChange(options.filter((_, i) => i !== index));
  };

  return (
    <div>
      {options.map((option, index) => (
        <fieldset key={index} role="group" style={{ marginBottom: '0.5rem' }}>
          <input
            type="text"
            placeholder={`Option ${index + 1}`}
            value={option}
            onChange={(e) => updateOption(index, e.target.value)}
            required
          />
          {options.length > 2 && (
            <button type="button" className="secondary" onClick={() => removeOption(index)}>
              Remove
            </button>
          )}
        </fieldset>
      ))}
      {options.length < 20 && (
        <button type="button" className="secondary outline" onClick={addOption}>
          + Add Option
        </button>
      )}
    </div>
  );
}
