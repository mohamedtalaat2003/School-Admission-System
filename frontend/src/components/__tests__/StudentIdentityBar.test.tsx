import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import { StudentProvider } from '../../context/StudentContext';
import { StudentIdentityBar } from '../StudentIdentityBar';

describe('StudentIdentityBar', () => {
  it('renders valid badge for valid 17-digit student id', () => {
    render(
      <StudentProvider>
        <StudentIdentityBar />
      </StudentProvider>
    );

    const input = screen.getByLabelText(/Student Birth Registration ID/i) as HTMLInputElement;
    fireEvent.change(input, { target: { value: '20152691234567890' } });

    expect(screen.getByText(/17-digit Valid/i)).toBeInTheDocument();
  });

  it('renders warning badge when student id is less than 17 digits', () => {
    render(
      <StudentProvider>
        <StudentIdentityBar />
      </StudentProvider>
    );

    const input = screen.getByLabelText(/Student Birth Registration ID/i) as HTMLInputElement;
    fireEvent.change(input, { target: { value: '12345' } });

    expect(screen.getByText(/5\/17 digits/i)).toBeInTheDocument();
  });

  it('restricts input to numeric characters only up to 17 digits', () => {
    render(
      <StudentProvider>
        <StudentIdentityBar />
      </StudentProvider>
    );

    const input = screen.getByLabelText(/Student Birth Registration ID/i) as HTMLInputElement;
    fireEvent.change(input, { target: { value: 'abc1234567890123456789extra' } });

    expect(input.value).toBe('12345678901234567');
    expect(input.value.length).toBe(17);
  });
});
